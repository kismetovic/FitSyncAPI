using FITSync.Domain.Models;

namespace FITSync.Infrastructure.Services.Interfaces
{
    public interface IAuthService
    {
        Task<string?> LoginAsync(string userNameOrEmail, string password, CancellationToken cancellationToken = default);
        Task<User?> RegisterAsync(string userName, string email, string password, string? name = null, string? surname = null, string? phoneNumber = null, CancellationToken cancellationToken = default);
        Task<bool> ForgotPasswordAsync(string email, string resetBaseUrl, CancellationToken cancellationToken = default);
        Task<bool> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken cancellationToken = default);
        Task<(bool Success, string? Error)> ChangePasswordAsync(int userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
    }
}
