using FITSync.Infrastructure.Services.Interfaces;

namespace FITSync.Infrastructure.Services
{
    public class UserActionService : IUserActionService
    {
        public Task LogActionAsync(int userId, string action, string? details = null, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
