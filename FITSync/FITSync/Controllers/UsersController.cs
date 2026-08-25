using FITSync.Contracts.Common;
using FITSync.Contracts.Users;
using FITSync.Domain.Definitions;
using FITSync.Infrastructure.Helpers;
using FITSync.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FITSync.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = RoleDefinition.Administrator)]
    public class UsersController : BaseCRUDController<UserResponse, UserInsertRequest, UserUpdateRequest>
    {
        private readonly IUserService _userService;
        private readonly ICaller _caller;

        public UsersController(IUserService service, ICaller caller) : base(service)
        {
            _userService = service;
            _caller = caller;
        }

        /// <summary>Paged, SQL-side search. Filtering no longer happens in memory.</summary>
        [HttpGet("search")]
        public async Task<ActionResult<PagedResult<UserResponse>>> Search(
            [FromQuery] UserSearchRequest request, CancellationToken cancellationToken = default)
            => Ok(await _userService.SearchAsync(request ?? new UserSearchRequest(), cancellationToken));

        [HttpGet("by-username/{userName}")]
        public async Task<ActionResult<UserResponse>> GetByUserName(string userName, CancellationToken cancellationToken = default)
        {
            var user = await _userService.GetByUserNameAsync(userName, cancellationToken);
            return user == null ? NotFound() : Ok(user);
        }

        [HttpGet("by-email/{email}")]
        public async Task<ActionResult<UserResponse>> GetByEmail(string email, CancellationToken cancellationToken = default)
        {
            var user = await _userService.GetByEmailAsync(email, cancellationToken);
            return user == null ? NotFound() : Ok(user);
        }

        /// <summary>
        /// Guards against an administrator locking themselves out, or deactivating the
        /// account they are currently signed in with.
        /// </summary>
        [HttpPut("{id:int}")]
        public override async Task<ActionResult<UserResponse>> UpdateAsync(int id, [FromBody] UserUpdateRequest request)
        {
            if (id == _caller.RequireUserId())
            {
                if (!request.Enabled)
                    return BadRequest(new { error = "SELF_DISABLE", message = "You cannot deactivate your own account." });

                if (!string.IsNullOrWhiteSpace(request.Role) && request.Role != RoleDefinition.Administrator)
                    return BadRequest(new { error = "SELF_DEMOTE", message = "You cannot remove your own administrator role." });
            }

            return await base.UpdateAsync(id, request);
        }

        [HttpDelete("{id:int}")]
        public override async Task<ActionResult> DeleteAsync(int id)
        {
            if (id == _caller.RequireUserId())
                return BadRequest(new { error = "SELF_DELETE", message = "You cannot delete your own account." });

            return await base.DeleteAsync(id);
        }
    }
}
