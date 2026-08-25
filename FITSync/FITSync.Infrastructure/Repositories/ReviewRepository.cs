using FITSync.Domain.Entities;
using FITSync.Infrastructure.Context;
using FITSync.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FITSync.Infrastructure.Repositories
{
    public class ReviewRepository : BaseRepository<Review>, IReviewRepository
    {
        public ReviewRepository(FitSyncDbContext context) : base(context)
        {
        }

        protected override IQueryable<Review> BaseQuery()
        {
            return _dbSet
                .Where(r => !r.IsDeleted)
                .Include(r => r.User)
                .Include(r => r.Training);
        }

        public async Task<List<Review>> GetByTrainingIdAsync(int trainingId, CancellationToken cancellationToken = default)
        {
            return await BaseQuery()
                .Where(r => r.TrainingId == trainingId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Review>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await BaseQuery()
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<(List<Review> Items, int TotalCount)> SearchAsync(
            int? trainingId, int? userId, string? searchTerm, int skip, int take,
            CancellationToken cancellationToken = default)
        {
            var query = BaseQuery();
            if (trainingId.HasValue)
                query = query.Where(r => r.TrainingId == trainingId.Value);
            if (userId.HasValue)
                query = query.Where(r => r.UserId == userId.Value);

            // Matched in SQL so the admin search still covers every page.
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                query = query.Where(r =>
                    (r.Comment != null && r.Comment.Contains(term)) ||
                    (r.Training != null && r.Training.Name.Contains(term)) ||
                    (r.User != null && (
                        (r.User.Name != null && r.User.Name.Contains(term)) ||
                        (r.User.Surname != null && r.User.Surname.Contains(term)) ||
                        (r.User.UserName != null && r.User.UserName.Contains(term)))));
            }

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
            return (items, total);
        }

        /// <summary>One review per attended reservation - this is the duplicate guard.</summary>
        public async Task<bool> ExistsForReservationAsync(int reservationId, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AnyAsync(r => !r.IsDeleted && r.ReservationId == reservationId, cancellationToken);
        }

        public override async Task<List<Review>> GetAsync()
        {
            return await BaseQuery().OrderByDescending(r => r.CreatedAt).ToListAsync();
        }

        public override async Task<Review?> GetByIdAsync(int id)
        {
            return await BaseQuery().FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<Dictionary<int, (double AverageRating, int ReviewCount)>> GetStatsByTrainingIdsAsync(
            IEnumerable<int> trainingIds, CancellationToken cancellationToken = default)
        {
            var ids = trainingIds.Distinct().ToList();
            if (ids.Count == 0) return new Dictionary<int, (double, int)>();

            var stats = await _dbSet
                .Where(r => ids.Contains(r.TrainingId) && !r.IsDeleted)
                .GroupBy(r => r.TrainingId)
                .Select(g => new { TrainingId = g.Key, Avg = g.Average(r => r.Rating), Count = g.Count() })
                .ToListAsync(cancellationToken);

            return stats.ToDictionary(x => x.TrainingId, x => (x.Avg, x.Count));
        }
    }
}
