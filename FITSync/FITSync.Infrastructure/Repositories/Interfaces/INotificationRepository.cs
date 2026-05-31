using FITSync.Domain.Entities;

namespace FITSync.Infrastructure.Repositories.Interfaces
{
    public interface INotificationRepository : IBaseRepository<Notification>
    {
        Task<List<Notification>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
        Task<List<Notification>> GetUnreadByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    }
}
