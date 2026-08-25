using FITSync.Domain.Entities;
using FITSync.Domain.Enums;

namespace FITSync.Infrastructure.Repositories.Interfaces
{
    public interface IPaymentRepository : IBaseRepository<Payment>
    {
        Task<Payment?> GetByReservationIdAsync(int reservationId, CancellationToken cancellationToken = default);
        Task<Payment?> GetCapturedByReservationIdAsync(int reservationId, CancellationToken cancellationToken = default);
        Task<Payment?> GetByProviderOrderIdAsync(string providerOrderId, CancellationToken cancellationToken = default);
        Task<Payment?> GetByTransactionIdAsync(string transactionId, CancellationToken cancellationToken = default);
        Task<List<Payment>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);

        Task<(List<Payment> Items, int TotalCount)> SearchAsync(
            int? userId, int? reservationId, PaymentStatus? status, PaymentProvider? provider,
            DateTime? fromDate, DateTime? toDate, string? searchTerm,
            int skip, int take, CancellationToken cancellationToken = default);

        Task<List<Payment>> GetCapturedInPeriodAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
        Task<decimal> GetTotalCapturedRevenueAsync(CancellationToken cancellationToken = default);

        /// <summary>Captured totals and per-provider counts, aggregated in SQL.</summary>
        Task<(decimal TotalRevenue, int CapturedCount, int PayPalCount, int CashCount)> GetSummaryAsync(
            CancellationToken cancellationToken = default);
    }
}
