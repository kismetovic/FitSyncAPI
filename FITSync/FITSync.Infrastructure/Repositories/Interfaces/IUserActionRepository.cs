using FITSync.Domain.Entities;
using FITSync.Domain.Enums;

namespace FITSync.Infrastructure.Repositories.Interfaces
{
    public interface IUserActionRepository : IBaseRepository<UserAction>
    {
        Task<List<UserAction>> GetByUserIdAsync(int userId, int limit = 200, CancellationToken cancellationToken = default);
        Task<Dictionary<int, int>> GetTrainingTypeWeightsAsync(int userId, CancellationToken cancellationToken = default);
        Task<List<int>> GetRecentlyViewedTrainingIdsAsync(int userId, int limit = 20, CancellationToken cancellationToken = default);
        Task<List<UserAction>> GetByActionTypeAsync(int userId, UserActionType actionType, CancellationToken cancellationToken = default);
    }
}
