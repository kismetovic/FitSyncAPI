using FITSync.Domain.Models;

namespace FITSync.Infrastructure.Services.Interfaces
{
    public interface IAuthService
    {
        Task<LoginOutcome> LoginAsync(string userNameOrEmail, string password, CancellationToken cancellationToken = default);
        Task<User?> RegisterAsync(string userName, string email, string password, string? name = null, string? surname = null, string? phoneNumber = null, CancellationToken cancellationToken = default);
        Task<bool> ForgotPasswordAsync(string email, string resetBaseUrl, CancellationToken cancellationToken = default);
        Task<bool> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken cancellationToken = default);
        Task<(bool Success, string? Error)> ChangePasswordAsync(int userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Distinguishes wrong credentials from a deactivated account, so the API can answer
    /// 401 in the first case and 403 with a clear message in the second.
    /// </summary>
    public class LoginOutcome
    {
        public bool Succeeded { get; private init; }
        public bool IsDisabled { get; private init; }
        public string? Token { get; private init; }
        public string? Error { get; private init; }
        public List<string> Roles { get; private init; } = new();

        public static LoginOutcome Success(string token, List<string> roles)
            => new() { Succeeded = true, Token = token, Roles = roles };

        public static LoginOutcome Failed(string error)
            => new() { Succeeded = false, Error = error };

        public static LoginOutcome Disabled(string error)
            => new() { Succeeded = false, IsDisabled = true, Error = error };
    }
}
