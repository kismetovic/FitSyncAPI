using FITSync.Contracts.Auth;
using FITSync.Contracts.Users;
using FITSync.Infrastructure.Helpers;
using FITSync.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FITSync.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IUserService _userService;
        private readonly ICaller _caller;

        public AuthController(IAuthService authService, IUserService userService, ICaller caller)
        {
            _authService = authService;
            _userService = userService;
            _caller = caller;
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserResponse>> GetMe(CancellationToken cancellationToken = default)
        {
            var user = await _userService.GetByIdAsync(_caller.RequireUserId());
            return user == null ? NotFound() : Ok(user);
        }

        /// <summary>
        /// A deactivated account is refused with 403 and a clear message rather than being
        /// issued a token. Wrong credentials still return a generic 401.
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken = default)
        {
            var outcome = await _authService.LoginAsync(request.UserNameOrEmail, request.Password, cancellationToken);

            if (outcome.IsDisabled)
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "ACCOUNT_DISABLED", message = outcome.Error });

            if (!outcome.Succeeded)
                return Unauthorized(new { error = "INVALID_CREDENTIALS", message = outcome.Error ?? "Invalid credentials." });

            return Ok(new LoginResponse { Token = outcome.Token!, Roles = outcome.Roles });
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken = default)
        {
            var user = await _authService.RegisterAsync(
                request.UserName,
                request.Email,
                request.Password,
                request.Name,
                request.Surname,
                request.PhoneNumber,
                cancellationToken);

            if (user == null)
                return BadRequest(new { error = "REGISTRATION_FAILED", message = "Registration failed. The username or email may already be in use." });

            return Ok(new RegisterResponse
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email
            });
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken = default)
        {
            var (ok, error) = await _authService.ChangePasswordAsync(
                _caller.RequireUserId(), request.CurrentPassword, request.NewPassword, cancellationToken);

            if (!ok)
                return BadRequest(new { error = "PASSWORD_CHANGE_FAILED", message = error ?? "Password change failed. Check your current password." });

            return Ok(new { message = "Password changed successfully." });
        }

        /// <summary>
        /// Always answers the same way, so the endpoint cannot be used to discover which
        /// email addresses are registered.
        /// </summary>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken = default)
        {
            await _authService.ForgotPasswordAsync(request.Email, request.ResetBaseUrl ?? "", cancellationToken);
            return Ok(new { message = "If the email exists, a reset link has been sent." });
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken = default)
        {
            var ok = await _authService.ResetPasswordAsync(request.Email, request.Token, request.NewPassword, cancellationToken);
            if (!ok)
                return BadRequest(new { error = "INVALID_RESET_TOKEN", message = "Invalid or expired reset token." });

            return Ok(new { message = "Password has been reset." });
        }
    }
}
