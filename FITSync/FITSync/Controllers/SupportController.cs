using FITSync.Contracts.Support;
using FITSync.Domain.Definitions;
using FITSync.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FITSync.WebAPI.Controllers
{
    /// <summary>
    /// The gym's support details. Readable by any signed-in user because the mobile
    /// help screen shows them; writable only by an administrator.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SupportController : ControllerBase
    {
        private readonly ISupportContactService _supportContactService;

        public SupportController(ISupportContactService supportContactService)
        {
            _supportContactService = supportContactService;
        }

        [HttpGet("contact")]
        [Authorize]
        public async Task<ActionResult<SupportContactResponse>> GetContact(CancellationToken cancellationToken = default)
            => Ok(await _supportContactService.GetAsync(cancellationToken));

        [HttpPut("contact")]
        [Authorize(Roles = RoleDefinition.Administrator)]
        public async Task<ActionResult<SupportContactResponse>> UpdateContact(
            [FromBody] SupportContactUpdateRequest request, CancellationToken cancellationToken = default)
            => Ok(await _supportContactService.UpdateAsync(request, cancellationToken));
    }
}
