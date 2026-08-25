using FITSync.Domain.Entities;
using FITSync.Domain.Enums;
using FITSync.Infrastructure.Context;
using FITSync.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FITSync.Infrastructure.Repositories
{
    public class UserActionRepository : BaseRepository<UserAction>, IUserActionRepository
    {
        public UserActionRepository(FitSyncDbContext context) : base(context)
        {
        }

        public async Task<List<UserAction>> GetByUserIdAsync(int userId, int limit = 200, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(a => a.UserId == userId && !a.IsDeleted)
                .OrderByDescending(a => a.OccurredAt)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<UserAction>> GetByActionTypeAsync(int userId, UserActionType actionType, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(a => a.UserId == userId && a.ActionType == actionType && !a.IsDeleted)
                .OrderByDescending(a => a.OccurredAt)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Per-training-type interest score, aggregated in SQL. Weights mirror
        /// docs/RECOMMENDER.md: a booking says far more than a page view.
        /// </summary>
        public async Task<Dictionary<int, int>> GetTrainingTypeWeightsAsync(int userId, CancellationToken cancellationToken = default)
        {
            var rows = await _dbSet
                .Where(a => a.UserId == userId && !a.IsDeleted && a.TrainingTypeId != null)
                .GroupBy(a => new { TrainingTypeId = a.TrainingTypeId!.Value, a.ActionType })
                .Select(g => new { g.Key.TrainingTypeId, g.Key.ActionType, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var weights = new Dictionary<int, int>();
            foreach (var row in rows)
            {
                var weight = row.ActionType switch
                {
                    UserActionType.CompletedTraining => 5,
                    UserActionType.ReservedTraining => 4,
                    UserActionType.ReviewedTraining => 3,
                    UserActionType.ViewedTraining => 1,
                    UserActionType.SearchedTraining => 1,
                    UserActionType.CancelledTraining => -2,
                    _ => 0
                };
                weights[row.TrainingTypeId] = weights.GetValueOrDefault(row.TrainingTypeId) + weight * row.Count;
            }
            return weights;
        }

        public async Task<List<int>> GetRecentlyViewedTrainingIdsAsync(int userId, int limit = 20, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(a => a.UserId == userId
                            && !a.IsDeleted
                            && a.ActionType == UserActionType.ViewedTraining
                            && a.TrainingId != null)
                .OrderByDescending(a => a.OccurredAt)
                .Select(a => a.TrainingId!.Value)
                .Distinct()
                .Take(limit)
                .ToListAsync(cancellationToken);
        }
    }
}
