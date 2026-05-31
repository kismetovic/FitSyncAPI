using FITSync.Domain.Definitions;
using FITSync.Domain.Models;
using FITSync.Infrastructure.Authentication;
using FITSync.Infrastructure.Repositories.Interfaces;
using FITSync.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace FITSync.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly UserManager<User> _userManager;
        private readonly IEmailNotificationService _emailNotificationService;

        public AuthService(
            IUserRepository userRepository,
            IJwtTokenGenerator jwtTokenGenerator,
            UserManager<User> userManager,
            IEmailNotificationService emailNotificationService)
        {
            _userRepository = userRepository;
            _jwtTokenGenerator = jwtTokenGenerator;
            _userManager = userManager;
            _emailNotificationService = emailNotificationService;
        }

        public async Task<string?> LoginAsync(string userNameOrEmail, string password, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByUserNameAsync(userNameOrEmail, cancellationToken)
                ?? await _userRepository.GetByEmailAsync(userNameOrEmail, cancellationToken);
            if (user == null)
                return null;

            var isValid = await _userManager.CheckPasswordAsync(user, password);
            if (!isValid)
                return null;

            var tokenResult = _jwtTokenGenerator.GenerateToken(user);
            return (tokenResult as dynamic)?.Token;
        }

        public async Task<User?> RegisterAsync(string userName, string email, string password, string? name = null, string? surname = null, string? phoneNumber = null, CancellationToken cancellationToken = default)
        {
            var user = new User
            {
                UserName = userName,
                Email = email,
                Name = name,
                Surname = surname,
                PhoneNumber = phoneNumber ?? "",
                EmailConfirmed = false,
                Enabled = true
            };
            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                return null;

            await _userManager.AddToRoleAsync(user, RoleDefinition.Client);

            try
            {
                await _emailNotificationService.SendWelcomeEmailAsync(user.Email ?? "", user.UserName ?? user.Email ?? "User", cancellationToken);
            }
            catch {  }
            return user;
        }

        public async Task<bool> ForgotPasswordAsync(string email, string resetBaseUrl, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
            if (user == null)
                return false;
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = $"{resetBaseUrl.TrimEnd('/')}/reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
            try
            {
                await _emailNotificationService.SendPasswordResetEmailAsync(user.Email ?? "", resetLink, cancellationToken);
            }
            catch { return false; }
            return true;
        }

        public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
            if (user == null)
                return false;
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            return result.Succeeded;
        }

        public async Task<(bool Success, string? Error)> ChangePasswordAsync(int userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return (false, "User not found.");
            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (!result.Succeeded)
            {
                var error = string.Join(" ", result.Errors.Select(e => e.Description));
                return (false, error);
            }
            return (true, null);
        }
    }
}
