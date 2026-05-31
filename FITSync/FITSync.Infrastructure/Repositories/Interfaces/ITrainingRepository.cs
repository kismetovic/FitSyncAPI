using FITSync.Domain.Entities;
using FITSync.Domain.Enums;

namespace FITSync.Infrastructure.Repositories.Interfaces
{
    public interface ITrainingRepository : IBaseRepository<Training>
    {
        Task<List<Training>> GetByTrainingTypeIdAsync(int trainingTypeId, CancellationToken cancellationToken = default);
        Task<List<Training>> SearchAsync(string? name, decimal? minPrice, decimal? maxPrice, int? trainingTypeId, TrainingDifficulty? difficulty, CancellationToken cancellationToken = default);
    }
}
