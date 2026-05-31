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

        public async Task<List<Training>> GetByTrainingTypeIdAsync(int trainingTypeId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(t => t.TrainingTypeId == trainingTypeId && !t.IsDeleted)
                .Include(t => t.TrainingType)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Training>> SearchAsync(string? name, decimal? minPrice, decimal? maxPrice, int? trainingTypeId, TrainingDifficulty? difficulty, CancellationToken cancellationToken = default)
        {
            var query = _dbSet.Where(t => !t.IsDeleted).Include(t => t.TrainingType).AsQueryable();
            if (!string.IsNullOrWhiteSpace(name))
                query = query.Where(t => t.Name.Contains(name));
            if (minPrice.HasValue)
                query = query.Where(t => t.Price >= minPrice.Value);
            if (maxPrice.HasValue)
                query = query.Where(t => t.Price <= maxPrice.Value);
            if (trainingTypeId.HasValue)
                query = query.Where(t => t.TrainingTypeId == trainingTypeId.Value);
            if (difficulty.HasValue)
                query = query.Where(t => t.Difficulty == difficulty.Value);
            return await query.ToListAsync(cancellationToken);
        }

        public override async Task<List<Training>> GetAsync()
        {
            return await _dbSet
                .Where(t => !t.IsDeleted)
                .Include(t => t.TrainingType)
                .ToListAsync();
        }

        public override async Task<Training?> GetByIdAsync(int id)
        {
            return await _dbSet
                .Include(t => t.TrainingType)
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
        }
    }
}
