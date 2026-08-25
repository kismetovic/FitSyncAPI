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

        protected override IQueryable<Notification> BaseQuery()
            => _dbSet.Where(n => !n.IsDeleted).OrderByDescending(n => n.CreatedAt);

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

        public async Task<(List<Notification> Items, int TotalCount)> GetPagedByUserIdAsync(
            int userId, int skip, int take, CancellationToken cancellationToken = default)
        {
            var query = _dbSet.Where(n => n.UserId == userId && !n.IsDeleted);
            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
            return (items, total);
        }

        /// <summary>Bulk mark-as-read, executed as a single UPDATE.</summary>
        public async Task<int> MarkAllReadAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(n => n.UserId == userId && !n.IsRead && !n.IsDeleted)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), cancellationToken);
        }

        public override async Task<Notification?> GetByIdAsync(int id)
        {
            return await _dbSet.FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted);
        }
    }
}
