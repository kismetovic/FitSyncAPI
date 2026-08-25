using FITSync.Contracts.Common;
using FITSync.Contracts.Notifications;
using FITSync.Domain.Definitions;
using FITSync.Infrastructure.Helpers;
using FITSync.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FITSync.WebAPI.Controllers
{
    /// <summary>
    /// A client can read and mark read only its own notifications. Creating, editing and
    /// listing everyone's notifications are administrator operations.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : BaseCRUDController<NotificationResponse, NotificationInsertRequest, NotificationUpdateRequest>
    {
        private readonly INotificationService _notificationService;
        private readonly IEmailNotificationService _emailNotificationService;
        private readonly ICaller _caller;

        public NotificationsController(
            INotificationService service,
            ICaller caller,
            IEmailNotificationService emailNotificationService) : base(service)
        {
            _notificationService = service;
            _caller = caller;
            _emailNotificationService = emailNotificationService;
        }

        // ------------------------------------------------------------------
        // Client-facing
        // ------------------------------------------------------------------

        [HttpGet("mine")]
        [Authorize]
        public async Task<ActionResult<List<NotificationResponse>>> GetMine(CancellationToken cancellationToken = default)
        {
            var list = await _notificationService.GetByUserIdAsync(_caller.RequireUserId(), cancellationToken);
            return Ok(list);
        }

        [HttpGet("mine/paged")]
        [Authorize]
        public async Task<ActionResult<PagedResult<NotificationResponse>>> GetMinePaged(
            [FromQuery] PagedRequest? paging, CancellationToken cancellationToken = default)
        {
            var result = await _notificationService.GetPagedByUserIdAsync(
                _caller.RequireUserId(), paging ?? new PagedRequest(), cancellationToken);
            return Ok(result);
        }

        [HttpGet("mine/unread")]
        [Authorize]
        public async Task<ActionResult<List<NotificationResponse>>> GetMineUnread(CancellationToken cancellationToken = default)
        {
            var list = await _notificationService.GetUnreadByUserIdAsync(_caller.RequireUserId(), cancellationToken);
            return Ok(list);
        }

        [HttpGet("mine/unread-count")]
        [Authorize]
        public async Task<ActionResult<int>> GetUnreadCount(CancellationToken cancellationToken = default)
        {
            var list = await _notificationService.GetUnreadByUserIdAsync(_caller.RequireUserId(), cancellationToken);
            return Ok(new { count = list.Count });
        }

        /// <summary>
        /// Purpose-built mark-as-read. Replaces the old pattern of GET-then-PUT the whole
        /// notification back, which let a client rewrite the message the server had sent it.
        /// </summary>
        [HttpPatch("{id:int}/read")]
        [Authorize]
        public async Task<ActionResult<NotificationResponse>> MarkRead(int id, CancellationToken cancellationToken = default)
        {
            var result = await _notificationService.MarkReadAsync(id, _caller.RequireUserId(), cancellationToken);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPatch("mine/read-all")]
        [Authorize]
        public async Task<ActionResult> MarkAllRead(CancellationToken cancellationToken = default)
        {
            var affected = await _notificationService.MarkAllReadAsync(_caller.RequireUserId(), cancellationToken);
            return Ok(new { updated = affected });
        }

        [HttpGet("{id:int}")]
        [Authorize]
        public override async Task<ActionResult<NotificationResponse>> GetByIdAsync(int id)
        {
            var notification = await _notificationService.GetByIdAsync(id);
            if (notification == null) return NotFound();

            if (!_caller.IsAdministrator && notification.UserId != _caller.RequireUserId())
                return Forbid();

            return Ok(notification);
        }

        // ------------------------------------------------------------------
        // Administrative
        // ------------------------------------------------------------------

        [HttpPost("send-payment-reminder")]
        [Authorize(Roles = RoleDefinition.Administrator)]
        public async Task<ActionResult> SendPaymentReminder(
            [FromBody] SendPaymentReminderRequest request, CancellationToken cancellationToken = default)
        {
            await _emailNotificationService.SendPaymentReminderToUserAsync(request.UserId, cancellationToken);
            return Ok(new { message = "If the user has unpaid reservations, a reminder has been created and queued." });
        }

        [HttpGet]
        [Authorize(Roles = RoleDefinition.Administrator)]
        public override async Task<ActionResult<List<NotificationResponse>>> GetAsync()
            => await base.GetAsync();

        [HttpGet("by-user/{userId:int}")]
        [Authorize]
        public async Task<ActionResult<List<NotificationResponse>>> GetByUserId(int userId, CancellationToken cancellationToken = default)
        {
            if (!_caller.IsAdministrator && _caller.RequireUserId() != userId)
                return Forbid();

            var list = await _notificationService.GetByUserIdAsync(userId, cancellationToken);
            return Ok(list);
        }

        [HttpPost]
        [Authorize(Roles = RoleDefinition.Administrator)]
        public override async Task<ActionResult<NotificationResponse>> InsertAsync([FromBody] NotificationInsertRequest request)
            => await base.InsertAsync(request);

        [HttpPut("{id:int}")]
        [Authorize(Roles = RoleDefinition.Administrator)]
        public override async Task<ActionResult<NotificationResponse>> UpdateAsync(int id, [FromBody] NotificationUpdateRequest request)
            => await base.UpdateAsync(id, request);

        [HttpDelete("{id:int}")]
        [Authorize]
        public override async Task<ActionResult> DeleteAsync(int id)
        {
            if (!_caller.IsAdministrator && !await _notificationService.IsOwnedByAsync(id, _caller.RequireUserId()))
                return Forbid();

            return await base.DeleteAsync(id);
        }
    }
}
