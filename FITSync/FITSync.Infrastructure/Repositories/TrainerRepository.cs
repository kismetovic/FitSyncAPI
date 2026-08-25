using FITSync.Domain.Entities;
using FITSync.Infrastructure.Context;
using FITSync.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FITSync.Infrastructure.Repositories
{
    public class TrainerRepository : BaseRepository<Trainer>, ITrainerRepository
    {
        public TrainerRepository(FitSyncDbContext context) : base(context)
        {
        }

        protected override IQueryable<Trainer> BaseQuery()
            => _dbSet.Where(t => !t.IsDeleted).Include(t => t.Availabilities);

        public override async Task<List<Trainer>> GetAsync()
            => await BaseQuery().OrderBy(t => t.LastName).ToListAsync();

        public override async Task<Trainer?> GetByIdAsync(int id)
            => await BaseQuery().FirstOrDefaultAsync(t => t.Id == id);

        public async Task<Trainer?> GetWithAvailabilityAsync(int trainerId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(t => !t.IsDeleted)
                .Include(t => t.Availabilities.Where(a => !a.IsDeleted))
                .FirstOrDefaultAsync(t => t.Id == trainerId, cancellationToken);
        }

        public async Task<List<TrainerAvailability>> GetAvailabilityAsync(int trainerId, CancellationToken cancellationToken = default)
        {
            return await _context.TrainerAvailabilities
                .Where(a => a.TrainerId == trainerId && !a.IsDeleted)
                .OrderBy(a => a.DayOfWeek).ThenBy(a => a.StartTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<TrainerAvailability> AddAvailabilityAsync(TrainerAvailability availability, CancellationToken cancellationToken = default)
        {
            await _context.TrainerAvailabilities.AddAsync(availability, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return availability;
        }

        public async Task<bool> RemoveAvailabilityAsync(int availabilityId, CancellationToken cancellationToken = default)
        {
            var entity = await _context.TrainerAvailabilities
                .FirstOrDefaultAsync(a => a.Id == availabilityId && !a.IsDeleted, cancellationToken);
            if (entity == null) return false;
            entity.IsDeleted = true;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
