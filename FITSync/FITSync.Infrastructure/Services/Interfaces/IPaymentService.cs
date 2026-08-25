using FITSync.Contracts.Common;
using FITSync.Contracts.Payments;

namespace FITSync.Infrastructure.Services.Interfaces
{
    public interface IPaymentService : IBaseCRUDService<PaymentResponse, PaymentInsertRequest, PaymentUpdateRequest>
    {
        Task<PagedResult<PaymentResponse>> SearchAsync(PaymentSearchRequest request, CancellationToken cancellationToken = default);

        /// <summary>Captured revenue and per-provider counts, aggregated in the database.</summary>
        Task<PaymentSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default);
        Task<PaymentResponse?> GetByReservationIdAsync(int reservationId, CancellationToken cancellationToken = default);
        Task<PaymentResponse?> GetByTransactionIdAsync(string transactionId, CancellationToken cancellationToken = default);
        Task<List<PaymentResponse>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);

        Task<CreatePayPalOrderResponse> CreatePayPalOrderAsync(int callerUserId, int reservationId, CancellationToken cancellationToken = default);

        Task<CapturePayPalOrderResponse> CapturePayPalOrderAsync(int callerUserId, string orderId, int reservationId, CancellationToken cancellationToken = default);

        Task<PaymentResponse> SelectCashPaymentAsync(int callerUserId, int reservationId, CancellationToken cancellationToken = default);

        Task<PaymentResponse> ConfirmCashPaymentAsync(int adminUserId, int reservationId, string? note, CancellationToken cancellationToken = default);

        // Paying for a bought package. Same PayPal and cash paths a booking uses.
        Task<CreatePayPalOrderResponse> CreateMembershipPayPalOrderAsync(int callerUserId, int membershipId, CancellationToken cancellationToken = default);

        Task<CapturePayPalOrderResponse> CaptureMembershipPayPalOrderAsync(int callerUserId, string orderId, int membershipId, CancellationToken cancellationToken = default);

        Task<PaymentResponse> SelectMembershipCashPaymentAsync(int callerUserId, int membershipId, CancellationToken cancellationToken = default);

        Task<PaymentResponse> ConfirmMembershipCashPaymentAsync(int adminUserId, int membershipId, string? note, CancellationToken cancellationToken = default);

        Task<bool> IsOwnedByAsync(int paymentId, int userId, CancellationToken cancellationToken = default);
    }
}
