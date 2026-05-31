using FITSync.Contracts.Reviews;
using FITSync.Infrastructure.Helpers;
using FITSync.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FITSync.WebAPI.Controllers
{
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

        [HttpPost]
        public override async Task<ActionResult<ReviewResponse>> InsertAsync([FromBody] ReviewInsertRequest request)
        {
            if (!_caller.IsAuthenticated) return Unauthorized();
            if (request.UserId == 0 && !string.IsNullOrEmpty(_caller.UserId))
                request.UserId = int.Parse(_caller.UserId);
            return await base.InsertAsync(request);
        }

        [HttpGet("mine")]
        public async Task<ActionResult<List<ReviewResponse>>> GetMine(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_caller.UserId))
                return Unauthorized();
            var list = await _reviewService.GetByUserIdAsync(int.Parse(_caller.UserId), cancellationToken);
            return Ok(list);
        }

        [HttpGet("by-training/{trainingId}")]
        public async Task<ActionResult<List<ReviewResponse>>> GetByTrainingId(int trainingId, CancellationToken cancellationToken = default)
        {
            var list = await _reviewService.GetByTrainingIdAsync(trainingId, cancellationToken);
            return Ok(list);
        }

        [HttpGet("by-user/{userId}")]
        public async Task<ActionResult<List<ReviewResponse>>> GetByUserId(int userId, CancellationToken cancellationToken = default)
        {
            if (!_caller.IsAuthenticated) return Unauthorized();
            if (_caller.UserId != userId.ToString() && !User.IsInRole("Administrator"))
                return Forbid();
            var list = await _reviewService.GetByUserIdAsync(userId, cancellationToken);
            return Ok(list);
        }
    }
}
