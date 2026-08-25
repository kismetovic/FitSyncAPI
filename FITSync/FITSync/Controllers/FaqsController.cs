using FITSync.Contracts.Common;
using FITSync.Contracts.Faqs;
using FITSync.Domain.Definitions;
using FITSync.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FITSync.WebAPI.Controllers
{
    /// <summary>
    /// Help content. Any signed-in user reads the active entries; only an
    /// administrator writes them. Same shape as the other catalogues: the client
    /// never sees a write endpoint it is not allowed to call.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class FaqsController : BaseCRUDController<FaqResponse, FaqInsertRequest, FaqUpdateRequest>
    {
        private readonly IFaqService _faqService;

        public FaqsController(IFaqService service) : base(service)
        {
            _faqService = service;
        }

        /// <summary>What the mobile help screen calls: active entries, in order.</summary>
        [HttpGet("active")]
        [Authorize]
        public async Task<ActionResult<List<FaqResponse>>> GetActive(CancellationToken cancellationToken = default)
            => Ok(await _faqService.GetActiveAsync(cancellationToken));

        [HttpGet]
        [Authorize(Roles = RoleDefinition.Administrator)]
        public override async Task<ActionResult<List<FaqResponse>>> GetAsync()
            => await base.GetAsync();

        [HttpGet("paged")]
        [Authorize(Roles = RoleDefinition.Administrator)]
        public async Task<ActionResult<PagedResult<FaqResponse>>> GetPaged([FromQuery] PagedRequest? paging)
        {
            var all = await _faqService.GetAsync();
            return Ok(Page(all, paging ?? new PagedRequest()));
        }

        [HttpGet("{id:int}")]
        [Authorize(Roles = RoleDefinition.Administrator)]
        public override async Task<ActionResult<FaqResponse>> GetByIdAsync(int id)
            => await base.GetByIdAsync(id);

        [HttpPost]
        [Authorize(Roles = RoleDefinition.Administrator)]
        public override async Task<ActionResult<FaqResponse>> InsertAsync([FromBody] FaqInsertRequest request)
            => await base.InsertAsync(request);

        [HttpPut("{id:int}")]
        [Authorize(Roles = RoleDefinition.Administrator)]
        public override async Task<ActionResult<FaqResponse>> UpdateAsync(int id, [FromBody] FaqUpdateRequest request)
            => await base.UpdateAsync(id, request);

        [HttpDelete("{id:int}")]
        [Authorize(Roles = RoleDefinition.Administrator)]
        public override async Task<ActionResult> DeleteAsync(int id)
            => await base.DeleteAsync(id);
    }
}
