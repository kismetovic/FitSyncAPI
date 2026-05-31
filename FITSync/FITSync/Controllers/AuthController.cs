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
            if (string.IsNullOrEmpty(_caller.UserId))
                return Unauthorized();
            var user = await _userService.GetByIdAsync(int.Parse(_caller.UserId));
            if (user == null)
                return NotFound();
            return Ok(user);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken = default)
        {
            var token = await _authService.LoginAsync(request.UserNameOrEmail, request.Password, cancellationToken);
            if (token == null)
                return Unauthorized("Invalid credentials.");
            return Ok(new LoginResponse { Token = token });
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
                return BadRequest("Registration failed (e.g. username or email already in use).");
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
            if (string.IsNullOrEmpty(_caller.UserId)) return Unauthorized();
            if (request.NewPassword != request.ConfirmNewPassword)
                return BadRequest(new { message = "New password and confirmation do not match." });
            var (ok, error) = await _authService.ChangePasswordAsync(
                int.Parse(_caller.UserId), request.CurrentPassword, request.NewPassword, cancellationToken);
            if (!ok)
                return BadRequest(new { message = error ?? "Password change failed. Check your current password." });
            return Ok(new { message = "Password changed successfully." });
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken = default)
        {
            var ok = await _authService.ForgotPasswordAsync(request.Email, request.ResetBaseUrl ?? "", cancellationToken);
            return Ok(new { Message = "If the email exists, a reset link has been sent." });
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken = default)
        {
            var ok = await _authService.ResetPasswordAsync(request.Email, request.Token, request.NewPassword, cancellationToken);
            if (!ok)
                return BadRequest("Invalid or expired reset token.");
            return Ok(new { Message = "Password has been reset." });
        }
    }
}
