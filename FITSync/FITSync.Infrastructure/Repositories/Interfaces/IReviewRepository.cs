using FITSync.Domain.Entities;

namespace FITSync.Infrastructure.Repositories.Interfaces
{
    public interface IReviewRepository : IBaseRepository<Review>
    {
        Task<List<Review>> GetByTrainingIdAsync(int trainingId, CancellationToken cancellationToken = default);
        Task<List<Review>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);

        Task<(List<Review> Items, int TotalCount)> SearchAsync(
            int? trainingId, int? userId, string? searchTerm, int skip, int take, CancellationToken cancellationToken = default);

        Task<bool> ExistsForReservationAsync(int reservationId, CancellationToken cancellationToken = default);

        Task<Dictionary<int, (double AverageRating, int ReviewCount)>> GetStatsByTrainingIdsAsync(
            IEnumerable<int> trainingIds, CancellationToken cancellationToken = default);
    }
}
