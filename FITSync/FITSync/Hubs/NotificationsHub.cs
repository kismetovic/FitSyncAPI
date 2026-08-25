using FITSync.Domain.Definitions;
using FITSync.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FITSync.WebAPI.Hubs
{
    /// <summary>
    /// Push channel for in-app notifications. Clients connect once after logging in and
    /// receive new notifications and unread counts without polling.
    /// Every connection is authenticated; the token is read from the access_token query
    /// parameter because a WebSocket handshake cannot carry an Authorization header.
    /// </summary>
    [Authorize]
    public class NotificationsHub : Hub
    {
        public const string Route = "/hubs/notifications";

        /// <summary>Group name a single user's connections are placed in.</summary>
        public static string UserGroup(int userId) => $"user-{userId}";

        public const string AdministratorsGroup = "administrators";

        public override async Task OnConnectedAsync()
        {
            var userId = Context.GetHttpContext().GetUserID();
            if (!string.IsNullOrEmpty(userId))
                await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(int.Parse(userId)));

            if (Context.User?.IsInRole(RoleDefinition.Administrator) == true)
                await Groups.AddToGroupAsync(Context.ConnectionId, AdministratorsGroup);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.GetHttpContext().GetUserID();
            if (!string.IsNullOrEmpty(userId))
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, UserGroup(int.Parse(userId)));

            if (Context.User?.IsInRole(RoleDefinition.Administrator) == true)
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, AdministratorsGroup);

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>Lets a client confirm the connection is alive without polling the API.</summary>
        public Task<string> Ping() => Task.FromResult("pong");
    }
}
