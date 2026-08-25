using FITSync.Domain.Definitions;
using FITSync.Domain.Models;
using FITSync.Infrastructure.Authentication;
using FITSync.Infrastructure.Exceptions;
using FITSync.Infrastructure.Notifications;
using FITSync.Infrastructure.Repositories.Interfaces;
using FITSync.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace FITSync.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly UserManager<User> _userManager;
        private readonly INotificationDispatcher _dispatcher;
        private readonly IEmailNotificationService _emailNotificationService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserRepository userRepository,
            IJwtTokenGenerator jwtTokenGenerator,
            UserManager<User> userManager,
            INotificationDispatcher dispatcher,
            IEmailNotificationService emailNotificationService,
            ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _jwtTokenGenerator = jwtTokenGenerator;
            _userManager = userManager;
            _dispatcher = dispatcher;
            _emailNotificationService = emailNotificationService;
            _logger = logger;
        }

        public async Task<LoginOutcome> LoginAsync(string userNameOrEmail, string password, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByUserNameAsync(userNameOrEmail, cancellationToken)
                ?? await _userRepository.GetByEmailAsync(userNameOrEmail, cancellationToken);

            if (user == null)
                return LoginOutcome.Failed("Invalid credentials.");

            var isValid = await _userManager.CheckPasswordAsync(user, password);
            if (!isValid)
                return LoginOutcome.Failed("Invalid credentials.");

            // A deactivated account must not be able to obtain a token. Without this check
            // the Enabled flag in the admin UI had no effect on anyone already registered.
            if (!user.Enabled)
                return LoginOutcome.Disabled("This account has been deactivated. Please contact the gym administration.");

            var token = _jwtTokenGenerator.GenerateToken(user);
            var roles = user.Roles?
                .Where(ur => ur.Role?.Name != null)
                .Select(ur => ur.Role!.Name!)
                .ToList() ?? new List<string>();

            return LoginOutcome.Success(token, roles);
        }

        public async Task<User?> RegisterAsync(
            string userName,
            string email,
            string password,
            string? name = null,
            string? surname = null,
            string? phoneNumber = null,
            CancellationToken cancellationToken = default)
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

            var roleResult = await _userManager.AddToRoleAsync(user, RoleDefinition.Client);
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                throw new BusinessRuleException("ROLE_ASSIGN_FAILED", string.Join("; ", roleResult.Errors.Select(e => e.Description)));
            }

            try
            {
                await _dispatcher.DispatchAsync(
                    user.Id,
                    NotificationTemplates.Welcome(user.Name ?? user.UserName ?? user.Email ?? "korisniče"),
                    user.Email,
                    true,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Welcome notification failed for user {UserId}; registration still succeeded.", user.Id);
            }

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
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not enqueue the password reset email.");
                return false;
            }
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

        public async Task<(bool Success, string? Error)> ChangePasswordAsync(
            int userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return (false, "User not found.");

            if (!user.Enabled)
                return (false, "This account has been deactivated.");

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (!result.Succeeded)
                return (false, string.Join(" ", result.Errors.Select(e => e.Description)));

            return (true, null);
        }
    }
}
