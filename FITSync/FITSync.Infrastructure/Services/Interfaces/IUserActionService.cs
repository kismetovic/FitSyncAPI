namespace FITSync.Infrastructure.Services.Interfaces
{
    public interface IUserActionService
    {
        Task LogActionAsync(int userId, string action, string? details = null, CancellationToken cancellationToken = default);
    }
}
