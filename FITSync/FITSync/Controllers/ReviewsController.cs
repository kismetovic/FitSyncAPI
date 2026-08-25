using FITSync.Contracts.Common;
using FITSync.Contracts.Reviews;
using FITSync.Domain.Definitions;
using FITSync.Infrastructure.Helpers;
using FITSync.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FITSync.WebAPI.Controllers
{
    /// <summary>
    /// Reviews are readable by any authenticated user, but writing one is tied to the
    /// caller's own completed reservation, and editing or deleting one is restricted to
    /// its author or an administrator.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewsController : BaseCRUDController<ReviewResponse, ReviewInsertRequest, ReviewUpdateRequest>
    {
        private readonly IReviewService _reviewService;
        private readonly ICaller _caller;

        public ReviewsController(IReviewService service, ICaller caller) : base(service)
        {
            _reviewService = service;
            _caller = caller;
        }

        /// <summary>
        /// Author comes from the token. The service verifies the reservation belongs to the
        /// caller, that the training was attended, and that it has not been reviewed already.
        /// </summary>
        [HttpPost]
        [Authorize]
        public override async Task<ActionResult<ReviewResponse>> InsertAsync([FromBody] ReviewInsertRequest request)
        {
            var created = await _reviewService.CreateForUserAsync(_caller.RequireUserId(), request);
            // Explicit location rather than CreatedAtAction: MVC strips the "Async" suffix
            // from action names, so nameof(GetByIdAsync) does not match a route.
            return Created($"/api/Reviews/{created.Id}", created);
        }

        [HttpGet]
        [Authorize]
        public override async Task<ActionResult<List<ReviewResponse>>> GetAsync()
        {
            var result = await _reviewService.SearchAsync(null, null, null, new PagedRequest());
            return Ok(result.Items);
        }

        [HttpGet("search")]
        [Authorize]
        public async Task<ActionResult<PagedResult<ReviewResponse>>> Search(
            [FromQuery] int? trainingId,
            [FromQuery] int? userId,
            [FromQuery] string? query,
            [FromQuery] PagedRequest? paging,
            CancellationToken cancellationToken = default)
        {
            var result = await _reviewService.SearchAsync(
                trainingId, userId, query, paging ?? new PagedRequest(), cancellationToken);
            return Ok(result);
        }

        [HttpGet("mine")]
        [Authorize]
        public async Task<ActionResult<List<ReviewResponse>>> GetMine(CancellationToken cancellationToken = default)
        {
            var list = await _reviewService.GetByUserIdAsync(_caller.RequireUserId(), cancellationToken);
            return Ok(list);
        }

        [HttpGet("by-training/{trainingId:int}")]
        [Authorize]
        public async Task<ActionResult<List<ReviewResponse>>> GetByTrainingId(int trainingId, CancellationToken cancellationToken = default)
        {
            var list = await _reviewService.GetByTrainingIdAsync(trainingId, cancellationToken);
            return Ok(list);
        }

        [HttpGet("by-user/{userId:int}")]
        [Authorize]
        public async Task<ActionResult<List<ReviewResponse>>> GetByUserId(int userId, CancellationToken cancellationToken = default)
        {
            if (!_caller.IsAdministrator && _caller.RequireUserId() != userId)
                return Forbid();

            var list = await _reviewService.GetByUserIdAsync(userId, cancellationToken);
            return Ok(list);
        }

        /// <summary>Author or administrator only.</summary>
        [HttpPut("{id:int}")]
        [Authorize]
        public override async Task<ActionResult<ReviewResponse>> UpdateAsync(int id, [FromBody] ReviewUpdateRequest request)
        {
            if (!_caller.IsAdministrator && !await _reviewService.IsOwnedByAsync(id, _caller.RequireUserId()))
                return Forbid();

            return await base.UpdateAsync(id, request);
        }

        /// <summary>Author or administrator only. A client cannot delete someone else's review.</summary>
        [HttpDelete("{id:int}")]
        [Authorize]
        public override async Task<ActionResult> DeleteAsync(int id)
        {
            if (!_caller.IsAdministrator && !await _reviewService.IsOwnedByAsync(id, _caller.RequireUserId()))
                return Forbid();

            return await base.DeleteAsync(id);
        }
    }
}
