using FITSync.Contracts.Common;
using FITSync.Contracts.Notifications;

namespace FITSync.Infrastructure.Services.Interfaces
{
    public interface INotificationService : IBaseCRUDService<NotificationResponse, NotificationInsertRequest, NotificationUpdateRequest>
    {
        Task<List<NotificationResponse>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
        Task<List<NotificationResponse>> GetUnreadByUserIdAsync(int userId, CancellationToken cancellationToken = default);
        Task<PagedResult<NotificationResponse>> GetPagedByUserIdAsync(int userId, PagedRequest paging, CancellationToken cancellationToken = default);

        Task<NotificationResponse?> MarkReadAsync(int notificationId, int userId, CancellationToken cancellationToken = default);
        Task<int> MarkAllReadAsync(int userId, CancellationToken cancellationToken = default);
        Task<bool> IsOwnedByAsync(int notificationId, int userId, CancellationToken cancellationToken = default);
    }
}
