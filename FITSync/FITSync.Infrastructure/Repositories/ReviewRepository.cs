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

        public async Task<List<Review>> GetByTrainingIdAsync(int trainingId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(r => r.TrainingId == trainingId && !r.IsDeleted)
                .Include(r => r.User)
                .Include(r => r.Training)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Review>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(r => r.UserId == userId && !r.IsDeleted)
                .Include(r => r.User)
                .Include(r => r.Training)
                .ToListAsync(cancellationToken);
        }

        public override async Task<List<Review>> GetAsync()
        {
            return await _dbSet
                .Where(r => !r.IsDeleted)
                .Include(r => r.User)
                .Include(r => r.Training)
                .ToListAsync();
        }

        public override async Task<Review?> GetByIdAsync(int id)
        {
            return await _dbSet
                .Include(r => r.User)
                .Include(r => r.Training)
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
        }

        public async Task<Dictionary<int, (double AverageRating, int ReviewCount)>> GetStatsByTrainingIdsAsync(IEnumerable<int> trainingIds, CancellationToken cancellationToken = default)
        {
            var ids = trainingIds.ToList();
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
