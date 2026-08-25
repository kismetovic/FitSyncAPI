using AutoMapper;
using FITSync.Contracts.Common;
using FITSync.Contracts.Payments;
using FITSync.Domain.Definitions;
using FITSync.Domain.Entities;
using FITSync.Domain.Enums;
using FITSync.Infrastructure.Context;
using FITSync.Infrastructure.Exceptions;
using FITSync.Infrastructure.Notifications;
using FITSync.Infrastructure.Repositories.Interfaces;
using FITSync.Infrastructure.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FITSync.Infrastructure.Services
{
    /// <summary>
    /// Owns the money. The server decides the amount, the server verifies the capture with
    /// PayPal, and the server is what marks a reservation paid. The client only ever names
    /// the reservation it wants to pay for.
    /// </summary>
    public class PaymentService : BaseCRUDService<Payment, PaymentResponse, PaymentInsertRequest, PaymentUpdateRequest>, IPaymentService
    {
        /// <summary>PayPal rejects most currencies; BAM is not supported, so orders are placed in EUR.</summary>
        private const string PayPalCurrency = "EUR";

        /// <summary>What the gym prices, reports and takes cash in.</summary>
        private const string GymCurrency = "BAM";

        /// <summary>
        /// The gym prices everything in BAM, which PayPal cannot charge. BAM is pegged to
        /// the euro by law at a fixed rate, so the conversion is a constant rather than a
        /// rate that has to be fetched.
        ///
        /// This used to be missing entirely: the BAM figure was handed to PayPal already
        /// labelled EUR, so a 17.00 BAM reservation was charged as 17.00 EUR - very nearly
        /// double what the client owed.
        /// </summary>
        private const decimal BamPerEur = 1.95583m;

        /// <summary>What PayPal will actually charge for a price quoted in BAM.</summary>
        private static decimal ToEur(decimal bam) =>
            Math.Round(bam / BamPerEur, 2, MidpointRounding.AwayFromZero);

        /// <summary>
        /// What the PayPal order's reference_id is set to, and what the capture is checked
        /// against afterwards. Kept in one place so the two can never drift apart.
        /// </summary>
        private static string ReservationReference(int reservationId) => $"reservation-{reservationId}";

        private static string MembershipReference(int membershipId) => $"membership-{membershipId}";

        private readonly IPaymentRepository _paymentRepository;
        private readonly IReservationRepository _reservationRepository;
        private readonly IReservationStatusHistoryRepository _historyRepository;
        private readonly IPayPalPaymentService _payPal;
        private readonly INotificationDispatcher _dispatcher;
        private readonly FitSyncDbContext _context;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(
            IPaymentRepository repository,
            IMapper mapper,
            IReservationRepository reservationRepository,
            IReservationStatusHistoryRepository historyRepository,
            IPayPalPaymentService payPal,
            INotificationDispatcher dispatcher,
            FitSyncDbContext context,
            ILogger<PaymentService> logger)
            : base(repository, mapper)
        {
            _paymentRepository = repository;
            _reservationRepository = reservationRepository;
            _historyRepository = historyRepository;
            _payPal = payPal;
            _dispatcher = dispatcher;
            _context = context;
            _logger = logger;
        }

        // ------------------------------------------------------------------
        // PayPal
        // ------------------------------------------------------------------

        public async Task<CreatePayPalOrderResponse> CreatePayPalOrderAsync(
            int callerUserId,
            int reservationId,
            CancellationToken cancellationToken = default)
        {
            var reservation = await LoadPayableReservationAsync(callerUserId, reservationId, cancellationToken);

            // Amount comes from the reservation row, which was priced by the server at booking.
            var amount = reservation.TotalPrice;
            if (amount <= 0)
                throw new BusinessRuleException("NOTHING_TO_PAY", "This reservation has no outstanding amount (it is covered by a monthly package).");

            // The reservation is priced in BAM; PayPal is charged the euro equivalent.
            var chargedEur = ToEur(amount);
            var order = await _payPal.CreateOrderAsync(
                chargedEur, PayPalCurrency, ReservationReference(reservation.Id), cancellationToken);

            // The pending attempt is recorded so a later capture can be matched to it and
            // so a replayed capture cannot create a second row.
            var existing = await _paymentRepository.GetByProviderOrderIdAsync(order.OrderId, cancellationToken);
            if (existing == null)
            {
                await _paymentRepository.InsertAsync(new Payment
                {
                    ReservationId = reservation.Id,
                    // Stored in BAM, like cash payments, so revenue never mixes units.
                    // What PayPal charged in EUR is recoverable from ProviderOrderId.
                    Amount = amount,
                    Currency = GymCurrency,
                    PaymentProvider = PaymentProvider.PayPal,
                    Status = PaymentStatus.Pending,
                    ProviderOrderId = order.OrderId,
                    TransactionId = string.Empty
                });
            }

            return new CreatePayPalOrderResponse
            {
                OrderId = order.OrderId,
                ApprovalUrl = order.ApprovalUrl,
                Amount = amount,
                Currency = GymCurrency,
                ChargedAmount = chargedEur,
                ChargedCurrency = PayPalCurrency,
                ReservationId = reservation.Id
            };
        }

        public async Task<CapturePayPalOrderResponse> CapturePayPalOrderAsync(
            int callerUserId,
            string orderId,
            int reservationId,
            CancellationToken cancellationToken = default)
        {
            var reservation = await LoadPayableReservationAsync(callerUserId, reservationId, cancellationToken, allowAlreadyPaid: true);

            // Idempotency: if this order was already captured, return the stored result
            // instead of charging or recording anything a second time.
            var known = await _paymentRepository.GetByProviderOrderIdAsync(orderId, cancellationToken);
            if (known is { Status: PaymentStatus.Captured })
            {
                if (known.ReservationId != reservation.Id)
                    throw new BusinessRuleException("ORDER_RESERVATION_MISMATCH", "This PayPal order belongs to a different reservation.");

                return BuildCaptureResponse(known, reservation, "COMPLETED");
            }

            if (await _paymentRepository.GetCapturedByReservationIdAsync(reservation.Id, cancellationToken) != null)
                throw new BusinessRuleException("ALREADY_PAID", "This reservation has already been paid.");

            var capture = await _payPal.CaptureOrderAsync(orderId, cancellationToken);

            // Verify what PayPal actually did before trusting it. The comparison is against
            // the euro figure the order was created with, not the BAM price of the booking.
            var expectedReference = ReservationReference(reservation.Id);
            var failure = Verify(capture, ToEur(reservation.TotalPrice), expectedReference);

            if (failure != null)
            {
                await RecordFailedAttemptAsync(reservation.Id, null, orderId, capture, failure, cancellationToken);
                throw new BusinessRuleException("PAYMENT_VERIFICATION_FAILED", failure);
            }

            Payment payment;
            await using (var transaction = await _context.Database.BeginTransactionAsync(cancellationToken))
            {
                payment = known ?? new Payment
                {
                    ReservationId = reservation.Id,
                    PaymentProvider = PaymentProvider.PayPal,
                    ProviderOrderId = orderId
                };

                // Recorded in BAM, matching what the client owed and what cash payments
                // store, so revenue reports never add euros to marks. The euro amount
                // PayPal captured is verified above and stays retrievable via the order id.
                payment.Amount = reservation.TotalPrice;
                payment.Currency = GymCurrency;
                payment.TransactionId = capture.TransactionId;
                payment.Status = PaymentStatus.Captured;
                payment.CapturedAt = DateTime.UtcNow;
                payment.FailureReason = null;

                if (payment.Id == 0)
                    await _context.Payments.AddAsync(payment, cancellationToken);

                // Payment and reservation status move together, in one transaction, so the
                // UI can never show a confirmed reservation the backend does not agree with.
                MoveReservationToPaid(reservation);

                await _context.SaveChangesAsync(cancellationToken);

                await _context.ReservationStatusHistories.AddAsync(new ReservationStatusHistory
                {
                    ReservationId = reservation.Id,
                    FromStatus = ReservationStatus.Approved,
                    ToStatus = ReservationStatus.Paid,
                    ChangedByUserId = callerUserId,
                    ChangedAt = DateTime.UtcNow,
                    Reason = $"PayPal capture {capture.TransactionId}"
                }, cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }

            await NotifyPaidAsync(reservation, payment, cancellationToken);

            return BuildCaptureResponse(payment, reservation, capture.Status);
        }

        // ------------------------------------------------------------------
        // Cash
        // ------------------------------------------------------------------

        /// <summary>
        /// A client picking "pay on arrival". This records the choice only. The reservation
        /// stays unpaid until an administrator confirms the cash was collected.
        /// </summary>
        public async Task<PaymentResponse> SelectCashPaymentAsync(int callerUserId, int reservationId, CancellationToken cancellationToken = default)
        {
            var reservation = await LoadPayableReservationAsync(callerUserId, reservationId, cancellationToken);

            var existing = await _context.Payments
                .FirstOrDefaultAsync(p => p.ReservationId == reservation.Id
                                          && p.PaymentProvider == PaymentProvider.Cash
                                          && p.Status == PaymentStatus.Pending
                                          && !p.IsDeleted, cancellationToken);

            if (existing != null)
                return _mapper.Map<PaymentResponse>(existing);

            var payment = await _paymentRepository.InsertAsync(new Payment
            {
                ReservationId = reservation.Id,
                Amount = reservation.TotalPrice,
                Currency = GymCurrency,
                PaymentProvider = PaymentProvider.Cash,
                Status = PaymentStatus.Pending,
                TransactionId = string.Empty
            });

            return _mapper.Map<PaymentResponse>(payment);
        }

        /// <summary>
        /// Administrator confirming cash at the desk. This is the only path by which a cash
        /// payment becomes real; a client cannot confirm their own cash payment.
        /// </summary>
        public async Task<PaymentResponse> ConfirmCashPaymentAsync(
            int adminUserId,
            int reservationId,
            string? note,
            CancellationToken cancellationToken = default)
        {
            var reservation = await _reservationRepository.GetByIdAsync(reservationId)
                ?? throw new NotFoundException("Reservation not found.");

            if (reservation.Status == ReservationStatus.Cancelled)
                throw new BusinessRuleException("RESERVATION_CANCELLED", "A cancelled reservation cannot be paid.");

            if (await _paymentRepository.GetCapturedByReservationIdAsync(reservation.Id, cancellationToken) != null)
                throw new BusinessRuleException("ALREADY_PAID", "This reservation has already been paid.");

            Payment payment;
            await using (var transaction = await _context.Database.BeginTransactionAsync(cancellationToken))
            {
                payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.ReservationId == reservation.Id
                                              && p.PaymentProvider == PaymentProvider.Cash
                                              && p.Status == PaymentStatus.Pending
                                              && !p.IsDeleted, cancellationToken)
                          ?? new Payment
                          {
                              ReservationId = reservation.Id,
                              PaymentProvider = PaymentProvider.Cash,
                              Currency = GymCurrency
                          };

                payment.Amount = reservation.TotalPrice;
                payment.Status = PaymentStatus.Captured;
                payment.CapturedAt = DateTime.UtcNow;
                payment.ConfirmedByUserId = adminUserId;
                payment.TransactionId = $"CASH-{reservation.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}";

                if (payment.Id == 0)
                    await _context.Payments.AddAsync(payment, cancellationToken);

                var from = reservation.Status;
                MoveReservationToPaid(reservation);

                await _context.SaveChangesAsync(cancellationToken);

                await _context.ReservationStatusHistories.AddAsync(new ReservationStatusHistory
                {
                    ReservationId = reservation.Id,
                    FromStatus = from,
                    ToStatus = ReservationStatus.Paid,
                    ChangedByUserId = adminUserId,
                    ChangedAt = DateTime.UtcNow,
                    Reason = note ?? "Gotovinska uplata potvrđena na recepciji"
                }, cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }

            await NotifyPaidAsync(reservation, payment, cancellationToken);

            return _mapper.Map<PaymentResponse>(payment);
        }

        // ------------------------------------------------------------------
        // Reads
        // ------------------------------------------------------------------

        public async Task<PagedResult<PaymentResponse>> SearchAsync(PaymentSearchRequest request, CancellationToken cancellationToken = default)
        {
            var (items, total) = await _paymentRepository.SearchAsync(
                request.UserId, request.ReservationId, request.Status, request.Provider,
                request.FromDate, request.ToDate, request.Query,
                request.Skip, request.Take, cancellationToken);

            return PagedResult<PaymentResponse>.Create(
                _mapper.Map<List<PaymentResponse>>(items), request.Page, request.PageSize, total);
        }

        public async Task<PaymentSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default)
        {
            var (revenue, captured, paypal, cash) = await _paymentRepository.GetSummaryAsync(cancellationToken);
            return new PaymentSummaryResponse
            {
                TotalRevenue = revenue,
                CapturedCount = captured,
                PayPalCount = paypal,
                CashCount = cash
            };
        }

        public async Task<PaymentResponse?> GetByReservationIdAsync(int reservationId, CancellationToken cancellationToken = default)
        {
            var entity = await _paymentRepository.GetByReservationIdAsync(reservationId, cancellationToken);
            return entity == null ? null : _mapper.Map<PaymentResponse>(entity);
        }

        public async Task<PaymentResponse?> GetByTransactionIdAsync(string transactionId, CancellationToken cancellationToken = default)
        {
            var entity = await _paymentRepository.GetByTransactionIdAsync(transactionId, cancellationToken);
            return entity == null ? null : _mapper.Map<PaymentResponse>(entity);
        }

        public async Task<List<PaymentResponse>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            var entities = await _paymentRepository.GetByUserIdAsync(userId, cancellationToken);
            return _mapper.Map<List<PaymentResponse>>(entities);
        }

        public async Task<bool> IsOwnedByAsync(int paymentId, int userId, CancellationToken cancellationToken = default)
        {
            return await _context.Payments
                .AnyAsync(p => p.Id == paymentId && !p.IsDeleted && p.Reservation.UserId == userId, cancellationToken);
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        /// <summary>
        /// Loads a reservation only if it belongs to the caller and is in a state that can
        /// still be paid. This is the ownership gate for the whole payment flow.
        /// </summary>
        // ------------------------------------------------------------------
        // Packages
        //
        // Buying a package used to be a single tap that handed out an active package
        // with no money involved at all: PurchaseAsync inserted the row and set
        // PricePaid from the catalogue. A package is now bought unpaid and only becomes
        // usable once a payment for it is captured, through exactly the same PayPal and
        // cash paths a booking goes through.
        // ------------------------------------------------------------------

        public async Task<CreatePayPalOrderResponse> CreateMembershipPayPalOrderAsync(
            int callerUserId,
            int membershipId,
            CancellationToken cancellationToken = default)
        {
            var membership = await LoadPayableMembershipAsync(callerUserId, membershipId, cancellationToken);

            // The amount comes from the package the server priced, never from the client.
            var amount = membership.PricePaid;
            if (amount <= 0)
                throw new BusinessRuleException("NOTHING_TO_PAY", "This package has no outstanding amount.");

            var chargedEur = ToEur(amount);
            var order = await _payPal.CreateOrderAsync(
                chargedEur, PayPalCurrency, MembershipReference(membership.Id), cancellationToken);

            var existing = await _paymentRepository.GetByProviderOrderIdAsync(order.OrderId, cancellationToken);
            if (existing == null)
            {
                await _paymentRepository.InsertAsync(new Payment
                {
                    UserMembershipId = membership.Id,
                    Amount = amount,
                    Currency = GymCurrency,
                    PaymentProvider = PaymentProvider.PayPal,
                    Status = PaymentStatus.Pending,
                    ProviderOrderId = order.OrderId,
                    TransactionId = string.Empty
                });
            }

            return new CreatePayPalOrderResponse
            {
                OrderId = order.OrderId,
                ApprovalUrl = order.ApprovalUrl,
                Amount = amount,
                Currency = GymCurrency,
                ChargedAmount = chargedEur,
                ChargedCurrency = PayPalCurrency,
                UserMembershipId = membership.Id
            };
        }

        public async Task<CapturePayPalOrderResponse> CaptureMembershipPayPalOrderAsync(
            int callerUserId,
            string orderId,
            int membershipId,
            CancellationToken cancellationToken = default)
        {
            var membership = await LoadPayableMembershipAsync(
                callerUserId, membershipId, cancellationToken, allowAlreadyPaid: true);

            var known = await _paymentRepository.GetByProviderOrderIdAsync(orderId, cancellationToken);
            if (known is { Status: PaymentStatus.Captured })
            {
                if (known.UserMembershipId != membership.Id)
                    throw new BusinessRuleException("ORDER_MEMBERSHIP_MISMATCH", "This PayPal order belongs to a different package.");

                return BuildMembershipCaptureResponse(known, membership, "COMPLETED");
            }

            if (await GetCapturedMembershipPaymentAsync(membership.Id, cancellationToken) != null)
                throw new BusinessRuleException("ALREADY_PAID", "This package has already been paid for.");

            var capture = await _payPal.CaptureOrderAsync(orderId, cancellationToken);

            // Same verification a booking gets: status, capture id, currency, amount and
            // the reference, before anything is written or the package becomes usable.
            var failure = Verify(capture, ToEur(membership.PricePaid), MembershipReference(membership.Id));
            if (failure != null)
            {
                await RecordFailedAttemptAsync(null, membership.Id, orderId, capture, failure, cancellationToken);
                throw new BusinessRuleException("PAYMENT_VERIFICATION_FAILED", failure);
            }

            Payment payment;
            await using (var transaction = await _context.Database.BeginTransactionAsync(cancellationToken))
            {
                payment = known ?? new Payment
                {
                    UserMembershipId = membership.Id,
                    PaymentProvider = PaymentProvider.PayPal,
                    ProviderOrderId = orderId
                };

                payment.Amount = membership.PricePaid;
                payment.Currency = GymCurrency;
                payment.TransactionId = capture.TransactionId;
                payment.Status = PaymentStatus.Captured;
                payment.CapturedAt = DateTime.UtcNow;
                payment.FailureReason = null;

                if (payment.Id == 0)
                    await _context.Payments.AddAsync(payment, cancellationToken);

                // The package becomes usable in the same transaction that records the
                // money, so it can never be usable without a captured payment behind it.
                membership.Status = MembershipStatus.Active;

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }

            await NotifyMembershipPaidAsync(membership, payment, cancellationToken);

            return BuildMembershipCaptureResponse(payment, membership, capture.Status);
        }

        /// <summary>
        /// A client choosing to pay for a package at the desk. Records the intent only:
        /// the package stays unusable until an administrator confirms the cash.
        /// </summary>
        public async Task<PaymentResponse> SelectMembershipCashPaymentAsync(
            int callerUserId, int membershipId, CancellationToken cancellationToken = default)
        {
            var membership = await LoadPayableMembershipAsync(callerUserId, membershipId, cancellationToken);

            var existing = await _context.Payments
                .FirstOrDefaultAsync(p => p.UserMembershipId == membership.Id
                                          && p.PaymentProvider == PaymentProvider.Cash
                                          && p.Status == PaymentStatus.Pending
                                          && !p.IsDeleted, cancellationToken);

            if (existing != null)
                return _mapper.Map<PaymentResponse>(existing);

            var payment = await _paymentRepository.InsertAsync(new Payment
            {
                UserMembershipId = membership.Id,
                Amount = membership.PricePaid,
                Currency = GymCurrency,
                PaymentProvider = PaymentProvider.Cash,
                Status = PaymentStatus.Pending,
                TransactionId = string.Empty
            });

            return _mapper.Map<PaymentResponse>(payment);
        }

        /// <summary>Administrator confirming cash taken for a package at the desk.</summary>
        public async Task<PaymentResponse> ConfirmMembershipCashPaymentAsync(
            int adminUserId, int membershipId, string? note, CancellationToken cancellationToken = default)
        {
            var membership = await _context.UserMemberships
                .FirstOrDefaultAsync(m => m.Id == membershipId && !m.IsDeleted, cancellationToken)
                ?? throw new NotFoundException("Membership not found.");

            if (membership.Status == MembershipStatus.Cancelled)
                throw new BusinessRuleException("MEMBERSHIP_CANCELLED", "A cancelled package cannot be paid for.");

            if (await GetCapturedMembershipPaymentAsync(membership.Id, cancellationToken) != null)
                throw new BusinessRuleException("ALREADY_PAID", "This package has already been paid for.");

            Payment payment;
            await using (var transaction = await _context.Database.BeginTransactionAsync(cancellationToken))
            {
                payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.UserMembershipId == membership.Id
                                              && p.PaymentProvider == PaymentProvider.Cash
                                              && p.Status == PaymentStatus.Pending
                                              && !p.IsDeleted, cancellationToken)
                          ?? new Payment
                          {
                              UserMembershipId = membership.Id,
                              PaymentProvider = PaymentProvider.Cash,
                              Currency = GymCurrency
                          };

                payment.Amount = membership.PricePaid;
                payment.Status = PaymentStatus.Captured;
                payment.CapturedAt = DateTime.UtcNow;
                payment.ConfirmedByUserId = adminUserId;
                payment.TransactionId = CashPackageReference(membership.Id);

                if (payment.Id == 0)
                    await _context.Payments.AddAsync(payment, cancellationToken);

                membership.Status = MembershipStatus.Active;

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }

            await NotifyMembershipPaidAsync(membership, payment, cancellationToken);

            return _mapper.Map<PaymentResponse>(payment);
        }

        private static string CashPackageReference(int membershipId) =>
            "CASH-PKG-" + membershipId + "-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");

        private async Task<UserMembership> LoadPayableMembershipAsync(
            int callerUserId, int membershipId, CancellationToken cancellationToken, bool allowAlreadyPaid = false)
        {
            var membership = await _context.UserMemberships
                .Include(m => m.MembershipPackage)
                .FirstOrDefaultAsync(m => m.Id == membershipId && !m.IsDeleted, cancellationToken)
                ?? throw new NotFoundException("Membership not found.");

            // Ownership is checked here, not in the controller, so no route can skip it.
            if (membership.UserId != callerUserId)
                throw new NotFoundException("Membership not found.");

            if (membership.Status == MembershipStatus.Cancelled)
                throw new BusinessRuleException("MEMBERSHIP_CANCELLED", "A cancelled package cannot be paid for.");

            if (!allowAlreadyPaid && membership.Status == MembershipStatus.Active)
                throw new BusinessRuleException("ALREADY_PAID", "This package has already been paid for.");

            return membership;
        }

        private Task<Payment?> GetCapturedMembershipPaymentAsync(int membershipId, CancellationToken cancellationToken)
            => _context.Payments.FirstOrDefaultAsync(
                p => p.UserMembershipId == membershipId
                     && p.Status == PaymentStatus.Captured
                     && !p.IsDeleted, cancellationToken);

        private async Task NotifyMembershipPaidAsync(
            UserMembership membership, Payment payment, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == membership.UserId, cancellationToken);
                await _dispatcher.DispatchAsync(
                    membership.UserId,
                    NotificationTemplates.MembershipPaid(membership, payment.Amount, payment.Currency),
                    user?.Email,
                    true,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Package {MembershipId} was paid but the notification failed.", membership.Id);
            }
        }

        private CapturePayPalOrderResponse BuildMembershipCaptureResponse(
            Payment payment, UserMembership membership, string rawStatus)
            => new()
            {
                TransactionId = payment.TransactionId,
                Status = rawStatus,
                PaymentStatus = payment.Status,
                Amount = payment.Amount,
                Currency = payment.Currency,
                UserMembershipId = membership.Id,
                MembershipStatus = membership.Status,
                Payment = _mapper.Map<PaymentResponse>(payment)
            };

        private async Task<Reservation> LoadPayableReservationAsync(
            int callerUserId,
            int reservationId,
            CancellationToken cancellationToken,
            bool allowAlreadyPaid = false)
        {
            var reservation = await _reservationRepository.GetByIdAsync(reservationId)
                ?? throw new NotFoundException("Reservation not found.");

            if (reservation.UserId != callerUserId)
                throw new ForbiddenOperationException("You can only pay for your own reservations.");

            if (reservation.Status == ReservationStatus.Cancelled)
                throw new BusinessRuleException("RESERVATION_CANCELLED", "A cancelled reservation cannot be paid.");

            if (reservation.Status == ReservationStatus.PendingApproval)
                throw new BusinessRuleException("AWAITING_APPROVAL", "This reservation is still waiting for trainer approval and cannot be paid yet.");

            if (!allowAlreadyPaid && reservation.Status is ReservationStatus.Paid or ReservationStatus.Completed)
                throw new BusinessRuleException("ALREADY_PAID", "This reservation has already been paid.");

            return reservation;
        }

        /// <summary>
        /// Cross-checks the PayPal capture against what we expect. Any mismatch means the
        /// payment is not accepted, no matter what the client claims.
        /// </summary>
        private static string? Verify(PayPalCaptureResult capture, decimal expectedAmount, string expectedReference)
        {
            if (!capture.IsCompleted)
                return $"PayPal reported status '{capture.Status}' instead of COMPLETED.";

            if (string.IsNullOrWhiteSpace(capture.TransactionId))
                return "PayPal did not return a capture id.";

            if (!string.Equals(capture.Currency, PayPalCurrency, StringComparison.OrdinalIgnoreCase))
                return $"Currency mismatch: expected {PayPalCurrency}, PayPal charged {capture.Currency}.";

            if (Math.Abs(capture.Amount - expectedAmount) > 0.01m)
                return $"Amount mismatch: expected {expectedAmount:0.00}, PayPal charged {capture.Amount:0.00}.";

            if (!string.IsNullOrWhiteSpace(capture.ReferenceId) &&
                !string.Equals(capture.ReferenceId, expectedReference, StringComparison.OrdinalIgnoreCase))
                return $"Reference mismatch: expected {expectedReference}, PayPal returned {capture.ReferenceId}.";

            return null;
        }

        private static void MoveReservationToPaid(Reservation reservation)
        {
            if (reservation.Status == ReservationStatus.Paid)
                return;

            if (!ReservationStatusTransitions.CanTransition(reservation.Status, ReservationStatus.Paid))
            {
                throw new BusinessRuleException(
                    "INVALID_STATUS_TRANSITION",
                    $"A reservation in status {reservation.Status} cannot be marked as paid.");
            }

            reservation.Status = ReservationStatus.Paid;
        }

        private async Task RecordFailedAttemptAsync(
            int? reservationId,
            int? membershipId,
            string orderId,
            PayPalCaptureResult capture,
            string reason,
            CancellationToken cancellationToken)
        {
            try
            {
                var payment = await _paymentRepository.GetByProviderOrderIdAsync(orderId, cancellationToken);
                if (payment == null)
                {
                    payment = new Payment
                    {
                        ReservationId = reservationId,
                        UserMembershipId = membershipId,
                        ProviderOrderId = orderId,
                        PaymentProvider = PaymentProvider.PayPal,
                        Currency = string.IsNullOrWhiteSpace(capture.Currency) ? PayPalCurrency : capture.Currency
                    };
                    await _context.Payments.AddAsync(payment, cancellationToken);
                }

                payment.Amount = capture.Amount;
                payment.TransactionId = capture.TransactionId;
                payment.Status = PaymentStatus.Failed;
                payment.FailureReason = reason;
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not record the failed PayPal attempt for order {OrderId}.", orderId);
            }
        }

        private async Task NotifyPaidAsync(Reservation reservation, Payment payment, CancellationToken cancellationToken)
        {
            try
            {
                await _dispatcher.DispatchToReservationOwnerAsync(
                    reservation,
                    NotificationTemplates.ReservationPaid(reservation, payment.Amount, payment.Currency),
                    true,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Payment for reservation {ReservationId} succeeded but the notification failed.", reservation.Id);
            }
        }

        private CapturePayPalOrderResponse BuildCaptureResponse(Payment payment, Reservation reservation, string rawStatus)
            => new()
            {
                TransactionId = payment.TransactionId,
                Status = rawStatus,
                PaymentStatus = payment.Status,
                Amount = payment.Amount,
                Currency = payment.Currency,
                ReservationId = reservation.Id,
                ReservationStatus = reservation.Status,
                Payment = _mapper.Map<PaymentResponse>(payment)
            };
    }
}
