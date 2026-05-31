using FITSync.Contracts.Trainings;
using FITSync.Infrastructure.Helpers;
using FITSync.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FITSync.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TrainingsController : BaseCRUDController<TrainingResponse, TrainingInsertRequest, TrainingUpdateRequest>
    {
        private readonly ITrainingService _trainingService;
        private readonly IRecommendationService _recommendationService;
        private readonly ICaller _caller;

        public TrainingsController(ITrainingService service, IRecommendationService recommendationService, ICaller caller) : base(service)
        {
            _trainingService = service;
            _recommendationService = recommendationService;
            _caller = caller;
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public override async Task<ActionResult<TrainingResponse>> InsertAsync([FromBody] TrainingInsertRequest request)
            => await base.InsertAsync(request);

        [HttpPut("{id}")]
        [Authorize(Roles = "Administrator")]
        public override async Task<ActionResult<TrainingResponse>> UpdateAsync(int id, [FromBody] TrainingUpdateRequest request)
            => await base.UpdateAsync(id, request);

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrator")]
        public override async Task<ActionResult> DeleteAsync(int id)
            => await base.DeleteAsync(id);

        [HttpGet("by-type/{trainingTypeId}")]
        public async Task<ActionResult<List<TrainingResponse>>> GetByTrainingTypeId(int trainingTypeId, CancellationToken cancellationToken = default)
        {
            var list = await _trainingService.GetByTrainingTypeIdAsync(trainingTypeId, cancellationToken);
            return Ok(list);
        }

        [HttpGet("search")]
        public async Task<ActionResult<List<TrainingResponse>>> Search([FromQuery] TrainingSearchRequest request, CancellationToken cancellationToken = default)
        {
            var list = await _trainingService.SearchAsync(request ?? new TrainingSearchRequest(), cancellationToken);
            return Ok(list);
        }

        [HttpGet("recommendations")]
        [Authorize]
        public async Task<ActionResult<List<TrainingResponse>>> GetRecommendations([FromQuery] int limit = 10, CancellationToken cancellationToken = default)
        {
            if (!_caller.IsAuthenticated || _caller.UserId == null)
                return Unauthorized();
            if (!int.TryParse(_caller.UserId, out var userId))
                return Unauthorized();
            var list = await _recommendationService.GetRecommendationsForUserAsync(userId, limit, cancellationToken);
            return Ok(list);
        }
    }
}

