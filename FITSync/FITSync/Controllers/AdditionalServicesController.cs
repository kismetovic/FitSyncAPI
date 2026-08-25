using FITSync.Contracts.AdditionalServices;
using FITSync.Contracts.Common;
using FITSync.Domain.Definitions;
using FITSync.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FITSync.WebAPI.Controllers
{
    /// <summary>
    /// The catalogue is readable by any signed-in client so it can be offered during
    /// booking, but only an administrator may change what the gym sells or what it costs.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AdditionalServicesController : BaseCRUDController<AdditionalServiceResponse, AdditionalServiceInsertRequest, AdditionalServiceUpdateRequest>
    {
        private readonly IAdditionalServiceService _additionalServiceService;

        public AdditionalServicesController(IAdditionalServiceService service) : base(service)
        {
            _additionalServiceService = service;
        }

        [HttpGet]
        [Authorize]
        public override async Task<ActionResult<List<AdditionalServiceResponse>>> GetAsync()
            => await base.GetAsync();

        [HttpGet("paged")]
        [Authorize]
        public async Task<ActionResult<PagedResult<AdditionalServiceResponse>>> GetPaged([FromQuery] PagedRequest? paging)
        {
            var all = await _additionalServiceService.GetAsync();
            return Ok(Page(all, paging ?? new PagedRequest()));
        }

        [HttpGet("{id:int}")]
        [Authorize]
        public override async Task<ActionResult<AdditionalServiceResponse>> GetByIdAsync(int id)
            => await base.GetByIdAsync(id);

        [HttpPost]
        [Authorize(Roles = RoleDefinition.Administrator)]
        public override async Task<ActionResult<AdditionalServiceResponse>> InsertAsync([FromBody] AdditionalServiceInsertRequest request)
            => await base.InsertAsync(request);

        [HttpPut("{id:int}")]
        [Authorize(Roles = RoleDefinition.Administrator)]
        public override async Task<ActionResult<AdditionalServiceResponse>> UpdateAsync(int id, [FromBody] AdditionalServiceUpdateRequest request)
            => await base.UpdateAsync(id, request);

        [HttpDelete("{id:int}")]
        [Authorize(Roles = RoleDefinition.Administrator)]
        public override async Task<ActionResult> DeleteAsync(int id)
            => await base.DeleteAsync(id);
    }
}
