using FITSync.Domain.Entities;
using FITSync.Infrastructure.Context;
using FITSync.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FITSync.Infrastructure.Repositories
{
    public class AdditionalServiceRepository : BaseRepository<AdditionalService>, IAdditionalServiceRepository
    {
        public AdditionalServiceRepository(FitSyncDbContext context) : base(context)
        {
        }

        public override async Task<List<AdditionalService>> GetAsync()
        {
            return await _dbSet
                .Where(a => !a.IsDeleted)
                .ToListAsync();
        }

        public override async Task<AdditionalService?> GetByIdAsync(int id)
        {
            return await _dbSet
                .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
        }
    }
}
