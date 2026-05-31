using FITSync.Contracts.Users;
using FITSync.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FITSync.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrator")]
    public class UsersController : BaseCRUDController<UserResponse, UserInsertRequest, UserUpdateRequest>
    {
        private readonly IUserService _userService;

        public UsersController(IUserService service) : base(service)
        {
            _userService = service;
        }

        [HttpGet("search")]
        public async Task<ActionResult<List<UserResponse>>> Search([FromQuery] string? name, CancellationToken cancellationToken = default)
        {
            var list = await _userService.GetAsync();
            if (!string.IsNullOrWhiteSpace(name))
            {
                list = list.Where(u =>
                    (u.Name?.Contains(name, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (u.UserName?.Contains(name, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (u.Email?.Contains(name, StringComparison.OrdinalIgnoreCase) ?? false)
                ).ToList();
            }
            return Ok(list);
        }

        [HttpGet("by-username/{userName}")]
        public async Task<ActionResult<UserResponse>> GetByUserName(string userName, CancellationToken cancellationToken = default)
        {
            var user = await _userService.GetByUserNameAsync(userName, cancellationToken);
            if (user == null)
                return NotFound();
            return Ok(user);
        }

        [HttpGet("by-email/{email}")]
        public async Task<ActionResult<UserResponse>> GetByEmail(string email, CancellationToken cancellationToken = default)
        {
            var user = await _userService.GetByEmailAsync(email, cancellationToken);
            if (user == null)
                return NotFound();
            return Ok(user);
        }
    }
}
