using FITSync.Domain.Entities;
using FITSync.Infrastructure.Context;
using FITSync.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FITSync.Infrastructure.Repositories
{
    public class TrainingTypeRepository : BaseRepository<TrainingType>, ITrainingTypeRepository
    {
        public TrainingTypeRepository(FitSyncDbContext context) : base(context)
        {
        }

        public override async Task<List<TrainingType>> GetAsync()
        {
            return await _dbSet
                .Where(tt => !tt.IsDeleted)
                .ToListAsync();
        }

        public override async Task<TrainingType?> GetByIdAsync(int id)
        {
            return await _dbSet
                .FirstOrDefaultAsync(tt => tt.Id == id && !tt.IsDeleted);
        }
    }
}
