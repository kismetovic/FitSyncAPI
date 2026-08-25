using FITSync.Domain.Entities;
using FITSync.Domain.Enums;
using FITSync.Infrastructure.Context;
using FITSync.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FITSync.Infrastructure.Repositories
{
    public class TrainingRepository : BaseRepository<Training>, ITrainingRepository
    {
        public TrainingRepository(FitSyncDbContext context) : base(context)
        {
        }

        protected override IQueryable<Training> BaseQuery()
        {
            return _dbSet
                .Where(t => !t.IsDeleted)
                .Include(t => t.TrainingType)
                .Include(t => t.Trainer);
        }

        public async Task<List<Training>> GetByTrainingTypeIdAsync(int trainingTypeId, CancellationToken cancellationToken = default)
        {
            return await BaseQuery()
                .Where(t => t.TrainingTypeId == trainingTypeId)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Batch variant used by the recommender: one query for many training types
        /// instead of one query per type.
        /// </summary>
        public async Task<List<Training>> GetByTrainingTypeIdsAsync(IEnumerable<int> trainingTypeIds, CancellationToken cancellationToken = default)
        {
            var ids = trainingTypeIds.Distinct().ToList();
            if (ids.Count == 0) return new List<Training>();

            return await BaseQuery()
                .Where(t => ids.Contains(t.TrainingTypeId))
                .ToListAsync(cancellationToken);
        }

        /// <summary>Batch fetch by id. Replaces the previous one-round-trip-per-id loop.</summary>
        public async Task<List<Training>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
        {
            var idList = ids.Distinct().ToList();
            if (idList.Count == 0) return new List<Training>();

            return await BaseQuery()
                .Where(t => idList.Contains(t.Id))
                .ToListAsync(cancellationToken);
        }

        public async Task<(List<Training> Items, int TotalCount)> SearchAsync(
            string? name,
            decimal? minPrice,
            decimal? maxPrice,
            int? trainingTypeId,
            int? trainerId,
            TrainingDifficulty? difficulty,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            var query = BaseQuery();

            if (!string.IsNullOrWhiteSpace(name))
                query = query.Where(t => t.Name.Contains(name));
            if (minPrice.HasValue)
                query = query.Where(t => t.Price >= minPrice.Value);
            if (maxPrice.HasValue)
                query = query.Where(t => t.Price <= maxPrice.Value);
            if (trainingTypeId.HasValue)
                query = query.Where(t => t.TrainingTypeId == trainingTypeId.Value);
            if (trainerId.HasValue)
                query = query.Where(t => t.TrainerId == trainerId.Value);
            if (difficulty.HasValue)
                query = query.Where(t => t.Difficulty == difficulty.Value);

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderBy(t => t.Name)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);

            return (items, total);
        }

        public async Task<int> CountAsync(CancellationToken cancellationToken = default)
            => await _dbSet.CountAsync(t => !t.IsDeleted, cancellationToken);

        public override async Task<List<Training>> GetAsync()
        {
            return await BaseQuery().OrderBy(t => t.Name).ToListAsync();
        }

        public override async Task<Training?> GetByIdAsync(int id)
        {
            return await BaseQuery().FirstOrDefaultAsync(t => t.Id == id);
        }
    }
}
