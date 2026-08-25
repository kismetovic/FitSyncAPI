using FITSync.Domain.Entities;

namespace FITSync.Infrastructure.Repositories.Interfaces
{
    public interface INotificationRepository : IBaseRepository<Notification>
    {
        Task<List<Notification>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
        Task<List<Notification>> GetUnreadByUserIdAsync(int userId, CancellationToken cancellationToken = default);

        Task<(List<Notification> Items, int TotalCount)> GetPagedByUserIdAsync(
            int userId, int skip, int take, CancellationToken cancellationToken = default);

        Task<int> MarkAllReadAsync(int userId, CancellationToken cancellationToken = default);
    }
}
