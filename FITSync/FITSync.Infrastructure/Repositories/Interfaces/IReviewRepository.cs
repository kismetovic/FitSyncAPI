using FITSync.Domain.Entities;

namespace FITSync.Infrastructure.Repositories.Interfaces
{
    public interface IReviewRepository : IBaseRepository<Review>
    {
        Task<List<Review>> GetByTrainingIdAsync(int trainingId, CancellationToken cancellationToken = default);
        Task<List<Review>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
        Task<Dictionary<int, (double AverageRating, int ReviewCount)>> GetStatsByTrainingIdsAsync(IEnumerable<int> trainingIds, CancellationToken cancellationToken = default);
    }
}
