using FITSync.Contracts.Notifications;
using FITSync.Infrastructure.Helpers;
using FITSync.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FITSync.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : BaseCRUDController<NotificationResponse, NotificationInsertRequest, NotificationUpdateRequest>
    {
        private readonly INotificationService _notificationService;
        private readonly ICaller _caller;

        private readonly IEmailNotificationService _emailNotificationService;

        public NotificationsController(INotificationService service, ICaller caller, IEmailNotificationService emailNotificationService) : base(service)
        {
            _notificationService = service;
            _caller = caller;
            _emailNotificationService = emailNotificationService;
        }

        [HttpPost("send-payment-reminder")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult> SendPaymentReminder([FromBody] SendPaymentReminderRequest request, CancellationToken cancellationToken = default)
        {
            await _emailNotificationService.SendPaymentReminderToUserAsync(request.UserId, cancellationToken);
            return Ok(new { Message = "If the user has unpaid reservations, a reminder email has been sent." });
        }

        [HttpGet("mine")]
        public async Task<ActionResult<List<NotificationResponse>>> GetMine(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_caller.UserId))
                return Unauthorized();
            var list = await _notificationService.GetByUserIdAsync(int.Parse(_caller.UserId), cancellationToken);
            return Ok(list);
        }

        [HttpGet("mine/unread")]
        public async Task<ActionResult<List<NotificationResponse>>> GetMineUnread(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_caller.UserId))
                return Unauthorized();
            var list = await _notificationService.GetUnreadByUserIdAsync(int.Parse(_caller.UserId), cancellationToken);
            return Ok(list);
        }

        [HttpGet("by-user/{userId}")]
        public async Task<ActionResult<List<NotificationResponse>>> GetByUserId(int userId, CancellationToken cancellationToken = default)
        {
            if (!_caller.IsAuthenticated) return Unauthorized();
            if (_caller.UserId != userId.ToString() && !User.IsInRole("Administrator"))
                return Forbid();
            var list = await _notificationService.GetByUserIdAsync(userId, cancellationToken);
            return Ok(list);
        }

        [HttpGet("by-user/{userId}/unread")]
        public async Task<ActionResult<List<NotificationResponse>>> GetUnreadByUserId(int userId, CancellationToken cancellationToken = default)
        {
            if (!_caller.IsAuthenticated) return Unauthorized();
            if (_caller.UserId != userId.ToString() && !User.IsInRole("Administrator"))
                return Forbid();
            var list = await _notificationService.GetUnreadByUserIdAsync(userId, cancellationToken);
            return Ok(list);
        }
    }
}
