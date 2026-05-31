using FITSync.Domain.Entities;
using FITSync.Infrastructure.Context;
using FITSync.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FITSync.Infrastructure.Repositories
{
    public class NotificationRepository : BaseRepository<Notification>, INotificationRepository
    {
        public NotificationRepository(FitSyncDbContext context) : base(context)
        {
        }

        public async Task<List<Notification>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(n => n.UserId == userId && !n.IsDeleted)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Notification>> GetUnreadByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(n => n.UserId == userId && !n.IsRead && !n.IsDeleted)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public override async Task<List<Notification>> GetAsync()
        {
            return await _dbSet
                .Where(n => !n.IsDeleted)
                .ToListAsync();
        }

        public override async Task<Notification?> GetByIdAsync(int id)
        {
            return await _dbSet
                .FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted);
        }
    }
}
