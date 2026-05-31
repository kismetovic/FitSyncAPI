using FITSync.Contracts.Payments;

namespace FITSync.Infrastructure.Services.Interfaces
{
    public interface IPaymentService : IBaseCRUDService<PaymentResponse, PaymentInsertRequest, PaymentUpdateRequest>
    {
        Task<PaymentResponse?> GetByReservationIdAsync(int reservationId, CancellationToken cancellationToken = default);
        Task<PaymentResponse?> GetByTransactionIdAsync(string transactionId, CancellationToken cancellationToken = default);
        Task<List<PaymentResponse>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    }
}
