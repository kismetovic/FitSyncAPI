using AutoMapper;
using FITSync.Contracts.Common;
using FITSync.Contracts.Notifications;
using FITSync.Domain.Entities;
using FITSync.Infrastructure.Context;
using FITSync.Infrastructure.Repositories.Interfaces;
using FITSync.Infrastructure.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FITSync.Infrastructure.Services
{
    public class NotificationService : BaseCRUDService<Notification, NotificationResponse, NotificationInsertRequest, NotificationUpdateRequest>, INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly INotificationPublisher _publisher;
        private readonly FitSyncDbContext _context;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            INotificationRepository repository,
            IMapper mapper,
            INotificationPublisher publisher,
            FitSyncDbContext context,
            ILogger<NotificationService> logger)
            : base(repository, mapper)
        {
            _notificationRepository = repository;
            _publisher = publisher;
            _context = context;
            _logger = logger;
        }

        public async Task<List<NotificationResponse>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            var entities = await _notificationRepository.GetByUserIdAsync(userId, cancellationToken);
            return _mapper.Map<List<NotificationResponse>>(entities);
        }

        public async Task<List<NotificationResponse>> GetUnreadByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            var entities = await _notificationRepository.GetUnreadByUserIdAsync(userId, cancellationToken);
            return _mapper.Map<List<NotificationResponse>>(entities);
        }

        public async Task<PagedResult<NotificationResponse>> GetPagedByUserIdAsync(int userId, PagedRequest paging, CancellationToken cancellationToken = default)
        {
            var (items, total) = await _notificationRepository.GetPagedByUserIdAsync(userId, paging.Skip, paging.Take, cancellationToken);
            return PagedResult<NotificationResponse>.Create(
                _mapper.Map<List<NotificationResponse>>(items), paging.Page, paging.PageSize, total);
        }

        /// <summary>
        /// Purpose-built mark-as-read. The mobile app used to GET the notification, flip a
        /// field and PUT the whole object back, which meant a client could rewrite the title
        /// and body of a notification the server had sent it.
        /// </summary>
        public async Task<NotificationResponse?> MarkReadAsync(int notificationId, int userId, CancellationToken cancellationToken = default)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && !n.IsDeleted, cancellationToken);

            if (notification == null || notification.UserId != userId)
                return null;

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync(cancellationToken);
                await PushUnreadCountAsync(userId, cancellationToken);
            }

            return _mapper.Map<NotificationResponse>(notification);
        }

        public async Task<int> MarkAllReadAsync(int userId, CancellationToken cancellationToken = default)
        {
            var affected = await _notificationRepository.MarkAllReadAsync(userId, cancellationToken);
            if (affected > 0)
                await PushUnreadCountAsync(userId, cancellationToken);
            return affected;
        }

        public async Task<bool> IsOwnedByAsync(int notificationId, int userId, CancellationToken cancellationToken = default)
        {
            return await _context.Notifications
                .AnyAsync(n => n.Id == notificationId && n.UserId == userId && !n.IsDeleted, cancellationToken);
        }

        private async Task PushUnreadCountAsync(int userId, CancellationToken cancellationToken)
        {
            try
            {
                var unread = await _notificationRepository.GetUnreadByUserIdAsync(userId, cancellationToken);
                await _publisher.PublishUnreadCountAsync(userId, unread.Count, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not push the unread count to user {UserId}.", userId);
            }
        }
    }
}
