using FITSync.Domain.Entities;
using FITSync.Infrastructure.Context;
using FITSync.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FITSync.Infrastructure.Repositories
{
    public class PaymentRepository : BaseRepository<Payment>, IPaymentRepository
    {
        public PaymentRepository(FitSyncDbContext context) : base(context)
        {
        }

        public async Task<Payment?> GetByReservationIdAsync(int reservationId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(p => p.Reservation)
                .FirstOrDefaultAsync(p => p.ReservationId == reservationId && !p.IsDeleted, cancellationToken);
        }

        public async Task<Payment?> GetByTransactionIdAsync(string transactionId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(p => p.Reservation)
                .FirstOrDefaultAsync(p => p.TransactionId == transactionId && !p.IsDeleted, cancellationToken);
        }

        public async Task<List<Payment>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(p => !p.IsDeleted && p.Reservation.UserId == userId)
                .Include(p => p.Reservation).ThenInclude(r => r.User)
                .Include(p => p.Reservation).ThenInclude(r => r.Training)
                .ToListAsync(cancellationToken);
        }

        public override async Task<List<Payment>> GetAsync()
        {
            return await _dbSet
                .Where(p => !p.IsDeleted)
                .Include(p => p.Reservation).ThenInclude(r => r.User)
                .Include(p => p.Reservation).ThenInclude(r => r.Training)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public override async Task<Payment?> GetByIdAsync(int id)
        {
            return await _dbSet
                .Include(p => p.Reservation).ThenInclude(r => r.User)
                .Include(p => p.Reservation).ThenInclude(r => r.Training)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        }
    }
}
