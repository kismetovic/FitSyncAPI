using FITSync.Domain.Entities;
using FITSync.Infrastructure.Context;
using FITSync.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FITSync.Infrastructure.Repositories
{
    public class SupportContactRepository : BaseRepository<SupportContact>, ISupportContactRepository
    {
        public SupportContactRepository(FitSyncDbContext context) : base(context)
        {
        }

        /// <summary>
        /// There is one gym, so there is one contact row. It is seeded, but this also
        /// creates it if the table is empty, so a fresh database never answers the
        /// help screen with nothing.
        /// </summary>
        public async Task<SupportContact> GetSingletonAsync(CancellationToken cancellationToken = default)
        {
            var existing = await _dbSet
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (existing != null) return existing;

            var created = new SupportContact
            {
                Email = "podrska@fitsync.ba",
                PhoneNumber = "+387 33 000 000",
                WorkingHours = "Pon – Pet, 08:00 – 20:00"
            };
            _dbSet.Add(created);
            await _context.SaveChangesAsync(cancellationToken);
            return created;
        }
    }
}
