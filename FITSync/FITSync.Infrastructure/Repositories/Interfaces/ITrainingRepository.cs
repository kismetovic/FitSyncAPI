using FITSync.Domain.Entities;
using FITSync.Domain.Enums;

namespace FITSync.Infrastructure.Repositories.Interfaces
{
    public interface ITrainingRepository : IBaseRepository<Training>
    {
        Task<List<Training>> GetByTrainingTypeIdAsync(int trainingTypeId, CancellationToken cancellationToken = default);

        Task<(List<Training> Items, int TotalCount)> SearchAsync(
            string? name, decimal? minPrice, decimal? maxPrice, int? trainingTypeId,
            int? trainerId, TrainingDifficulty? difficulty,
            int skip, int take, CancellationToken cancellationToken = default);

        Task<List<Training>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);

        Task<List<Training>> GetByTrainingTypeIdsAsync(IEnumerable<int> trainingTypeIds, CancellationToken cancellationToken = default);

        Task<int> CountAsync(CancellationToken cancellationToken = default);
    }
}
