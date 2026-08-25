using FITSync.Domain.Entities;
using FITSync.Infrastructure.Context;
using FITSync.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FITSync.Infrastructure.Repositories
{
    public class FaqRepository : BaseRepository<Faq>, IFaqRepository
    {
        public FaqRepository(FitSyncDbContext context) : base(context)
        {
        }

        public override async Task<List<Faq>> GetAsync()
        {
            return await _dbSet
                .Where(f => !f.IsDeleted)
                .OrderBy(f => f.SortOrder).ThenBy(f => f.Id)
                .ToListAsync();
        }

        public override async Task<Faq?> GetByIdAsync(int id)
        {
            return await _dbSet.FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);
        }

        public async Task<List<Faq>> GetActiveAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(f => !f.IsDeleted && f.IsActive)
                .OrderBy(f => f.SortOrder).ThenBy(f => f.Id)
                .ToListAsync(cancellationToken);
        }
    }
}
