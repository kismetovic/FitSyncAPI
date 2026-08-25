using AutoMapper;
using FITSync.Contracts.Common;
using FITSync.Contracts.Reservations;
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
    public class ReservationService : BaseCRUDService<Reservation, ReservationResponse, ReservationInsertRequest, ReservationUpdateRequest>, IReservationService
    {
        private readonly IReservationRepository _reservationRepository;
        private readonly ITrainingRepository _trainingRepository;
        private readonly IReservationStatusHistoryRepository _historyRepository;
        private readonly IUserMembershipRepository _userMembershipRepository;
        private readonly ITrainerService _trainerService;
        private readonly INotificationDispatcher _dispatcher;
        private readonly IUserActionService _userActionService;
        private readonly FitSyncDbContext _context;
        private readonly ILogger<ReservationService> _logger;

        public ReservationService(
            IReservationRepository repository,
            IMapper mapper,
            FitSyncDbContext context,
            ITrainingRepository trainingRepository,
            IReservationStatusHistoryRepository historyRepository,
            IUserMembershipRepository userMembershipRepository,
            ITrainerService trainerService,
            INotificationDispatcher dispatcher,
            IUserActionService userActionService,
            ILogger<ReservationService> logger)
            : base(repository, mapper)
        {
            _reservationRepository = repository;
            _context = context;
            _trainingRepository = trainingRepository;
            _historyRepository = historyRepository;
            _userMembershipRepository = userMembershipRepository;
            _trainerService = trainerService;
            _dispatcher = dispatcher;
            _userActionService = userActionService;
            _logger = logger;
        }

        // ------------------------------------------------------------------
        // Creation
        // ------------------------------------------------------------------

        /// <summary>
        /// The only way a reservation is created. The owner is the authenticated caller and
        /// the initial status is chosen here, never taken from the request.
        /// </summary>
        public async Task<ReservationResponse> CreateForUserAsync(
            int ownerUserId,
            ReservationInsertRequest request,
            CancellationToken cancellationToken = default)
        {
            var training = await _trainingRepository.GetByIdAsync(request.TrainingId)
                ?? throw new BusinessRuleException("TRAINING_NOT_FOUND", "The selected training does not exist.");

            var start = request.ReservationDate;
            var end = start.AddMinutes(training.DurationMinutes);

            if (start <= DateTime.UtcNow)
                throw new BusinessRuleException("DATE_IN_PAST", "A reservation cannot be made for a time that has already passed.");

            await EnsureNoOverlapAsync(ownerUserId, start, end, null, cancellationToken);
            await EnsureCapacityAsync(training, start, null, cancellationToken);

            var availability = await _trainerService.CheckAvailabilityAsync(training.TrainerId, start, end, cancellationToken);

            // A slot outside the trainer's hours is only allowed when the client explicitly
            // asked for it, and it always lands in PendingApproval with a surcharge.
            if (!availability.IsWithinAvailability && !request.RequestOutsideAvailability)
            {
                throw new BusinessRuleException(
                    "OUTSIDE_AVAILABILITY",
                    "The selected slot is outside the trainer's working hours. " +
                    "Enable the out-of-hours option to send a request for approval; an extra fee applies.");
            }

            var isOutside = !availability.IsWithinAvailability;
            var surcharge = isOutside ? availability.Surcharge : 0m;

            var membership = await ResolveMembershipAsync(ownerUserId, request, training, start, cancellationToken);
            var servicesTotal = await SumAdditionalServicesAsync(request.AdditionalServiceIds, cancellationToken);

            // Price is computed here and frozen on the row. The payment flow reads it back;
            // it never accepts an amount from the client.
            var basePrice = membership != null ? 0m : training.Price;
            var totalPrice = basePrice + servicesTotal + surcharge;

            var initialStatus = isOutside ? ReservationStatus.PendingApproval : ReservationStatus.Initial;

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            var reservation = new Reservation
            {
                UserId = ownerUserId,
                TrainingId = training.Id,
                ReservationDate = start,
                ReservationType = request.ReservationType,
                Status = initialStatus,
                TotalPrice = totalPrice,
                IsOutsideTrainerAvailability = isOutside,
                OutsideAvailabilitySurcharge = surcharge,
                UserMembershipId = membership?.Id
            };

            await _context.Reservations.AddAsync(reservation, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            await AttachAdditionalServicesAsync(reservation.Id, request.AdditionalServiceIds, cancellationToken);

            if (membership != null)
            {
                membership.SessionsUsed += 1;
                if (membership.SessionsRemaining == 0)
                    membership.Status = MembershipStatus.Expired;
                await _context.SaveChangesAsync(cancellationToken);
            }

            await WriteHistoryAsync(reservation.Id, initialStatus, initialStatus, ownerUserId,
                isOutside ? "Zahtjev van dostupnosti trenera" : "Rezervacija kreirana", cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            var saved = await _reservationRepository.GetByIdAsync(reservation.Id);
            await NotifyCreatedAsync(saved!, isOutside, cancellationToken);

            await _userActionService.LogActionAsync(
                ownerUserId, UserActionType.ReservedTraining, training.Id, training.TrainingTypeId,
                $"status={initialStatus}", cancellationToken);

            return ToResponse(saved!);
        }

        /// <summary>
        /// Base CRUD insert is not a legal entry point for reservations, because it has no
        /// authenticated owner. Controllers call CreateForUserAsync instead.
        /// </summary>
        public override Task<ReservationResponse> InsertAsync(ReservationInsertRequest request)
            => throw new BusinessRuleException(
                "OWNER_REQUIRED",
                "Reservations must be created through the authenticated endpoint so the owner comes from the token.");

        // ------------------------------------------------------------------
        // State machine actions
        // ------------------------------------------------------------------

        public async Task<ReservationResponse?> ApproveAsync(int id, int actingUserId, CancellationToken cancellationToken = default)
        {
            var reservation = await _reservationRepository.GetByIdAsync(id);
            if (reservation == null) return null;

            await TransitionAsync(reservation, ReservationStatus.Approved, actingUserId, "Odobreno", cancellationToken);

            await _dispatcher.DispatchToReservationOwnerAsync(
                reservation, NotificationTemplates.ReservationApproved(reservation), true, cancellationToken);

            return ToResponse(reservation);
        }

        public async Task<ReservationResponse?> CancelAsync(
            int id,
            int actingUserId,
            bool actingAsStaff,
            string reason,
            CancellationToken cancellationToken = default)
        {
            var reservation = await _reservationRepository.GetByIdAsync(id);
            if (reservation == null) return null;

            if (string.IsNullOrWhiteSpace(reason))
                throw new BusinessRuleException("REASON_REQUIRED", "A cancellation reason is required.");

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            var from = reservation.Status;
            EnsureTransitionAllowed(from, ReservationStatus.Cancelled);

            reservation.Status = ReservationStatus.Cancelled;
            reservation.CancelledAt = DateTime.UtcNow;
            reservation.CancelledByUserId = actingUserId;
            reservation.CancellationReason = reason.Trim();

            // A cancelled session goes back into the monthly package.
            if (reservation.UserMembershipId.HasValue)
            {
                var membership = await _context.UserMemberships
                    .FirstOrDefaultAsync(m => m.Id == reservation.UserMembershipId.Value, cancellationToken);
                if (membership != null && membership.SessionsUsed > 0)
                {
                    membership.SessionsUsed -= 1;
                    if (membership.Status == MembershipStatus.Expired && membership.EndDate >= DateTime.UtcNow)
                        membership.Status = MembershipStatus.Active;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            await WriteHistoryAsync(reservation.Id, from, ReservationStatus.Cancelled, actingUserId, reason, cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            // Both sides are told: the client, and the staff who need to free the slot.
            await _dispatcher.DispatchToReservationOwnerAsync(
                reservation, NotificationTemplates.ReservationCancelled(reservation, reason, actingAsStaff), true, cancellationToken);

            if (!actingAsStaff)
            {
                var clientName = reservation.User?.Name ?? reservation.User?.UserName ?? $"Korisnik #{reservation.UserId}";
                await _dispatcher.DispatchToAdministratorsAsync(
                    NotificationTemplates.ReservationCancelledStaffCopy(reservation, reason, clientName), false, cancellationToken);
            }

            await _userActionService.LogActionAsync(
                reservation.UserId, UserActionType.CancelledTraining, reservation.TrainingId,
                reservation.Training?.TrainingTypeId, reason, cancellationToken);

            return ToResponse(reservation);
        }

        public async Task<ReservationResponse?> CompleteAsync(int id, int actingUserId, string? note, CancellationToken cancellationToken = default)
        {
            var reservation = await _reservationRepository.GetByIdAsync(id);
            if (reservation == null) return null;

            await TransitionAsync(reservation, ReservationStatus.Completed, actingUserId, note ?? "Trening odrađen", cancellationToken);

            reservation.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            await _dispatcher.DispatchToReservationOwnerAsync(
                reservation, NotificationTemplates.ReservationCompleted(reservation), false, cancellationToken);

            await _userActionService.LogActionAsync(
                reservation.UserId, UserActionType.CompletedTraining, reservation.TrainingId,
                reservation.Training?.TrainingTypeId, null, cancellationToken);

            return ToResponse(reservation);
        }

        // ------------------------------------------------------------------
        // Reads
        // ------------------------------------------------------------------

        public async Task<PagedResult<ReservationResponse>> SearchAsync(ReservationSearchRequest request, CancellationToken cancellationToken = default)
        {
            var (items, total) = await _reservationRepository.SearchAsync(
                request.UserId, request.TrainingId, request.Status,
                request.FromDate, request.ToDate, request.Query,
                request.Skip, request.Take, cancellationToken);

            return PagedResult<ReservationResponse>.Create(
                items.Select(ToResponse).ToList(), request.Page, request.PageSize, total);
        }

        public async Task<List<ReservationResponse>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            var entities = await _reservationRepository.GetByUserIdAsync(userId, cancellationToken);
            return entities.Select(ToResponse).ToList();
        }

        public async Task<List<ReservationResponse>> GetByTrainingIdAsync(int trainingId, CancellationToken cancellationToken = default)
        {
            var entities = await _reservationRepository.GetByTrainingIdAsync(trainingId, cancellationToken);
            return entities.Select(ToResponse).ToList();
        }

        public override async Task<ReservationResponse?> GetByIdAsync(int id)
        {
            var entity = await _reservationRepository.GetByIdAsync(id);
            return entity == null ? null : ToResponse(entity);
        }

        public override async Task<List<ReservationResponse>> GetAsync()
        {
            var entities = await _reservationRepository.GetAsync();
            return entities.Select(ToResponse).ToList();
        }

        public async Task<bool> IsOwnedByAsync(int reservationId, int userId, CancellationToken cancellationToken = default)
        {
            return await _context.Reservations
                .AnyAsync(r => r.Id == reservationId && r.UserId == userId && !r.IsDeleted, cancellationToken);
        }

        // ------------------------------------------------------------------
        // Administrative edit (schedule only - never status, never ownership)
        // ------------------------------------------------------------------

        public override async Task<ReservationResponse?> UpdateAsync(int id, ReservationUpdateRequest request)
        {
            var reservation = await _reservationRepository.GetByIdAsync(id);
            if (reservation == null) return null;

            if (reservation.Status is ReservationStatus.Cancelled or ReservationStatus.Completed)
                throw new BusinessRuleException("RESERVATION_CLOSED", "A cancelled or completed reservation can no longer be edited.");

            var training = await _trainingRepository.GetByIdAsync(request.TrainingId)
                ?? throw new BusinessRuleException("TRAINING_NOT_FOUND", "The selected training does not exist.");

            var start = request.ReservationDate;
            var end = start.AddMinutes(training.DurationMinutes);

            await EnsureNoOverlapAsync(reservation.UserId, start, end, reservation.Id, default);
            await EnsureCapacityAsync(training, start, reservation.Id, default);

            var availability = await _trainerService.CheckAvailabilityAsync(training.TrainerId, start, end);
            var surcharge = availability.IsWithinAvailability ? 0m : availability.Surcharge;
            var servicesTotal = await SumAdditionalServicesAsync(request.AdditionalServiceIds, default);
            var basePrice = reservation.UserMembershipId.HasValue ? 0m : training.Price;

            reservation.ReservationDate = start;
            reservation.ReservationType = request.ReservationType;
            reservation.TrainingId = training.Id;
            reservation.IsOutsideTrainerAvailability = !availability.IsWithinAvailability;
            reservation.OutsideAvailabilitySurcharge = surcharge;
            reservation.TotalPrice = basePrice + servicesTotal + surcharge;

            await ReplaceAdditionalServicesAsync(reservation.Id, request.AdditionalServiceIds, default);
            await _context.SaveChangesAsync();

            var refreshed = await _reservationRepository.GetByIdAsync(reservation.Id);
            return ToResponse(refreshed!);
        }

        /// <summary>
        /// Deleting a reservation is not a supported operation: a reservation must stay
        /// visible as cancelled, with its reason and audit trail. Callers use CancelAsync.
        /// </summary>
        public override Task<bool> DeleteAsync(int id)
            => throw new BusinessRuleException(
                "USE_CANCEL_ENDPOINT",
                "Reservations are cancelled, not deleted. Use PATCH /api/Reservations/{id}/cancel with a reason.");

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        /// <summary>
        /// Correct interval overlap: two bookings clash when
        /// newStart &lt; existingEnd AND existingStart &lt; newEnd. The previous check compared
        /// the distance between start times against a duration, which missed cases where the
        /// new training was longer than the existing one.
        /// </summary>
        private async Task EnsureNoOverlapAsync(int userId, DateTime start, DateTime end, int? excludeId, CancellationToken cancellationToken)
        {
            var overlapping = await _reservationRepository.GetOverlappingForUserAsync(userId, start, end, excludeId, cancellationToken);
            if (overlapping.Count == 0) return;

            var clash = overlapping[0];
            throw new BusinessRuleException(
                "TIME_CONFLICT",
                $"You already have a reservation between {clash.ReservationDate:dd.MM.yyyy HH:mm} and " +
                $"{clash.ReservationDate.AddMinutes(clash.Training?.DurationMinutes ?? 0):HH:mm}.");
        }

        /// <summary>Capacity is enforced here, not only shown in the mobile UI.</summary>
        private async Task EnsureCapacityAsync(Training training, DateTime slotStart, int? excludeId, CancellationToken cancellationToken)
        {
            var taken = await _reservationRepository.CountActiveForSlotAsync(training.Id, slotStart, excludeId, cancellationToken);
            if (taken >= training.MaxCapacity)
            {
                throw new BusinessRuleException(
                    "CAPACITY_FULL",
                    $"This term is full ({taken}/{training.MaxCapacity} places taken). Please choose another time.");
            }
        }

        private async Task<UserMembership?> ResolveMembershipAsync(
            int userId,
            ReservationInsertRequest request,
            Training training,
            DateTime start,
            CancellationToken cancellationToken)
        {
            if (request.ReservationType != ReservationType.Monthly)
                return null;

            var membership = request.UserMembershipId.HasValue
                ? await _userMembershipRepository.GetUsableAsync(userId, request.UserMembershipId.Value, start, cancellationToken)
                : await _userMembershipRepository.FindUsableForTrainingTypeAsync(userId, training.TrainingTypeId, start, cancellationToken);

            if (membership == null)
            {
                throw new BusinessRuleException(
                    "NO_USABLE_MEMBERSHIP",
                    "A monthly reservation needs an active package with sessions left that covers this training type. " +
                    "Purchase a package first, or book this training as a one-time reservation.");
            }

            var packageType = membership.MembershipPackage?.TrainingTypeId;
            if (packageType.HasValue && packageType.Value != training.TrainingTypeId)
            {
                throw new BusinessRuleException(
                    "MEMBERSHIP_TYPE_MISMATCH",
                    "The selected package does not cover this training type.");
            }

            return membership;
        }

        private async Task<decimal> SumAdditionalServicesAsync(List<int> serviceIds, CancellationToken cancellationToken)
        {
            if (serviceIds == null || serviceIds.Count == 0) return 0m;

            var ids = serviceIds.Distinct().ToList();
            var services = await _context.AdditionalServices
                .Where(a => ids.Contains(a.Id) && !a.IsDeleted)
                .ToListAsync(cancellationToken);

            if (services.Count != ids.Count)
                throw new BusinessRuleException("INVALID_ADDITIONAL_SERVICE", "One or more selected additional services do not exist.");

            return services.Sum(a => a.Price);
        }

        private async Task AttachAdditionalServicesAsync(int reservationId, List<int> serviceIds, CancellationToken cancellationToken)
        {
            if (serviceIds == null || serviceIds.Count == 0) return;

            foreach (var serviceId in serviceIds.Distinct())
            {
                await _context.ReservationServices.AddAsync(new Domain.Entities.ReservationService
                {
                    ReservationId = reservationId,
                    AdditionalServiceId = serviceId
                }, cancellationToken);
            }
            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task ReplaceAdditionalServicesAsync(int reservationId, List<int> serviceIds, CancellationToken cancellationToken)
        {
            var existing = await _context.ReservationServices
                .Where(rs => rs.ReservationId == reservationId)
                .ToListAsync(cancellationToken);
            _context.ReservationServices.RemoveRange(existing);
            await _context.SaveChangesAsync(cancellationToken);

            await AttachAdditionalServicesAsync(reservationId, serviceIds, cancellationToken);
        }

        private static void EnsureTransitionAllowed(ReservationStatus from, ReservationStatus to)
        {
            if (!ReservationStatusTransitions.CanTransition(from, to))
            {
                var allowed = ReservationStatusTransitions.AllowedTargets(from);
                var allowedText = allowed.Count == 0 ? "none (final state)" : string.Join(", ", allowed);
                throw new BusinessRuleException(
                    "INVALID_STATUS_TRANSITION",
                    $"A reservation in status {from} cannot move to {to}. Allowed: {allowedText}.");
            }
        }

        private async Task TransitionAsync(
            Reservation reservation,
            ReservationStatus to,
            int actingUserId,
            string? reason,
            CancellationToken cancellationToken)
        {
            var from = reservation.Status;
            EnsureTransitionAllowed(from, to);

            reservation.Status = to;
            await _context.SaveChangesAsync(cancellationToken);
            await WriteHistoryAsync(reservation.Id, from, to, actingUserId, reason, cancellationToken);
        }

        private async Task WriteHistoryAsync(
            int reservationId,
            ReservationStatus from,
            ReservationStatus to,
            int? actingUserId,
            string? reason,
            CancellationToken cancellationToken)
        {
            await _context.ReservationStatusHistories.AddAsync(new ReservationStatusHistory
            {
                ReservationId = reservationId,
                FromStatus = from,
                ToStatus = to,
                ChangedByUserId = actingUserId,
                ChangedAt = DateTime.UtcNow,
                Reason = reason
            }, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// The message must match the actual status. A PendingApproval request is announced
        /// as received-and-waiting, never as a confirmation.
        /// </summary>
        private async Task NotifyCreatedAsync(Reservation reservation, bool isOutside, CancellationToken cancellationToken)
        {
            try
            {
                await _dispatcher.DispatchToReservationOwnerAsync(
                    reservation, NotificationTemplates.ReservationCreated(reservation), true, cancellationToken);

                if (isOutside)
                {
                    var clientName = reservation.User?.Name ?? reservation.User?.UserName ?? $"Korisnik #{reservation.UserId}";
                    await _dispatcher.DispatchToAdministratorsAsync(
                        NotificationTemplates.OutsideAvailabilityRequested(reservation, clientName), false, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                // The reservation is already committed; a notification problem must not undo it.
                _logger.LogWarning(ex, "Could not notify about reservation {ReservationId}.", reservation.Id);
            }
        }

        private ReservationResponse ToResponse(Reservation reservation)
        {
            var response = _mapper.Map<ReservationResponse>(reservation);
            response.AllowedNextStatuses = ReservationStatusTransitions.AllowedTargets(reservation.Status).ToList();
            response.IsPaid = reservation.Payments?.Any(p => !p.IsDeleted && p.Status == PaymentStatus.Captured) ?? false;
            return response;
        }
    }
}
