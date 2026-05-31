using FITSync.Domain.Entities;

namespace FITSync.Infrastructure.Repositories.Interfaces
{
    public interface IPaymentRepository : IBaseRepository<Payment>
    {
        Task<Payment?> GetByReservationIdAsync(int reservationId, CancellationToken cancellationToken = default);
        Task<Payment?> GetByTransactionIdAsync(string transactionId, CancellationToken cancellationToken = default);
        Task<List<Payment>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    }
}
