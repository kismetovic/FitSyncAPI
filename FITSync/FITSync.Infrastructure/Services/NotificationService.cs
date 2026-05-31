using AutoMapper;
using FITSync.Contracts.Notifications;
using FITSync.Domain.Entities;
using FITSync.Infrastructure.Repositories.Interfaces;
using FITSync.Infrastructure.Services.Interfaces;

namespace FITSync.Infrastructure.Services
{
    public class NotificationService : BaseCRUDService<Notification, NotificationResponse, NotificationInsertRequest, NotificationUpdateRequest>, INotificationService
    {
        private readonly INotificationRepository _notificationRepository;

        public NotificationService(INotificationRepository repository, IMapper mapper)
            : base(repository, mapper)
        {
            _notificationRepository = repository;
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
    }
}
