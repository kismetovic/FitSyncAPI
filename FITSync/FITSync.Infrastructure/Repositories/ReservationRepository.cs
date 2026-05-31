using FITSync.Domain.Entities;
using FITSync.Infrastructure.Context;
using FITSync.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FITSync.Infrastructure.Repositories
{
    public class ReservationRepository : BaseRepository<Reservation>, IReservationRepository
    {
        public ReservationRepository(FitSyncDbContext context) : base(context)
        {
        }

        public async Task<List<Reservation>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(r => r.UserId == userId && !r.IsDeleted)
                .Include(r => r.Training)
                .Include(r => r.User)
                .Include(r => r.ReservationServices)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Reservation>> GetByTrainingIdAsync(int trainingId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(r => r.TrainingId == trainingId && !r.IsDeleted)
                .Include(r => r.Training)
                .Include(r => r.User)
                .ToListAsync(cancellationToken);
        }

        public override async Task<List<Reservation>> GetAsync()
        {
            return await _dbSet
                .Where(r => !r.IsDeleted)
                .Include(r => r.Training)
                .Include(r => r.User)
                .ToListAsync();
        }

        public override async Task<Reservation?> GetByIdAsync(int id)
        {
            return await _dbSet
                .Include(r => r.Training)
                .Include(r => r.User)
                .Include(r => r.ReservationServices)
                .Include(r => r.Payment)
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
        }
    }
}
