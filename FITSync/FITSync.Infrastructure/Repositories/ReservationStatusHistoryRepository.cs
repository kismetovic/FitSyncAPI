using FITSync.Domain.Entities;
using FITSync.Infrastructure.Context;
using FITSync.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FITSync.Infrastructure.Repositories
{
    public class ReservationStatusHistoryRepository : BaseRepository<ReservationStatusHistory>, IReservationStatusHistoryRepository
    {
        public ReservationStatusHistoryRepository(FitSyncDbContext context) : base(context)
        {
        }

        public async Task<List<ReservationStatusHistory>> GetByReservationIdAsync(int reservationId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(h => h.ReservationId == reservationId && !h.IsDeleted)
                .OrderBy(h => h.ChangedAt)
                .ToListAsync(cancellationToken);
        }
    }
}
