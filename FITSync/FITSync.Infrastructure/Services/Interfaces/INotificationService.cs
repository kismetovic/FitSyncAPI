using FITSync.Contracts.Notifications;

namespace FITSync.Infrastructure.Services.Interfaces
{
    public interface INotificationService : IBaseCRUDService<NotificationResponse, NotificationInsertRequest, NotificationUpdateRequest>
    {
        Task<List<NotificationResponse>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
        Task<List<NotificationResponse>> GetUnreadByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    }
}
