using FITSync.Contracts.Dashboard;

namespace FITSync.Infrastructure.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardStatsResponse> GetStatsAsync(CancellationToken cancellationToken = default);
        Task<List<DashboardTrainingStatsResponse>> GetTrainingStatsAsync(CancellationToken cancellationToken = default);
    }
}
