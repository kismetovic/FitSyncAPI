using FITSync.Contracts.Reports;

namespace FITSync.Infrastructure.Services.Interfaces
{
    public interface IReportService
    {
        Task<ReservationReportResponse> GetReservationReportAsync(ReservationReportRequest request, CancellationToken cancellationToken = default);
        Task<RevenueReportResponse> GetRevenueReportAsync(RevenueReportRequest request, CancellationToken cancellationToken = default);
    }
}
