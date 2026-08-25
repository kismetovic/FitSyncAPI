using FITSync.Contracts.Common;
using FITSync.Contracts.Trainings;
using FITSync.Domain.Definitions;
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
        private readonly IUserActionService _userActionService;
        private readonly ICaller _caller;

        public TrainingsController(
            ITrainingService service,
            IRecommendationService recommendationService,
            IUserActionService userActionService,
            ICaller caller) : base(service)
        {
            _trainingService = service;
            _recommendationService = recommendationService;
            _userActionService = userActionService;
            _caller = caller;
        }

        [HttpGet]
        [Authorize]
        public override async Task<ActionResult<List<TrainingResponse>>> GetAsync()
            => await base.GetAsync();

        /// <summary>
        /// Opening a training is a recommender signal, so the view is recorded here rather
        /// than depending on the mobile app to report it separately.
        /// </summary>
        [HttpGet("{id:int}")]
        [Authorize]
        public override async Task<ActionResult<TrainingResponse>> GetByIdAsync(int id)
        {
            var training = await _trainingService.GetByIdAsync(id);
            if (training == null) return NotFound();

            if (_caller.UserIdValue is { } userId)
                await _userActionService.LogTrainingViewAsync(userId, id);

            return Ok(training);
        }

        [HttpGet("by-type/{trainingTypeId:int}")]
        [Authorize]
        public async Task<ActionResult<List<TrainingResponse>>> GetByTrainingTypeId(int trainingTypeId, CancellationToken cancellationToken = default)
        {
            var list = await _trainingService.GetByTrainingTypeIdAsync(trainingTypeId, cancellationToken);
            return Ok(list);
        }

        /// <summary>Paged, filtered search. PageSize is capped server-side at 100.</summary>
        [HttpGet("search")]
        [Authorize]
        public async Task<ActionResult<PagedResult<TrainingResponse>>> Search(
            [FromQuery] TrainingSearchRequest request, CancellationToken cancellationToken = default)
        {
            request ??= new TrainingSearchRequest();
            var result = await _trainingService.SearchAsync(request, cancellationToken);

            if (_caller.UserIdValue is { } userId && !string.IsNullOrWhiteSpace(request.Name))
                await _userActionService.LogSearchAsync(userId, request.Name, request.TrainingTypeId, cancellationToken);

            return Ok(result);
        }

        /// <summary>
        /// Personalised recommendations. Each item carries a score, the strategy that
        /// produced it and a reason string the app shows to the user.
        /// </summary>
        [HttpGet("recommendations")]
        [Authorize]
        public async Task<ActionResult<List<RecommendedTrainingResponse>>> GetRecommendations(
            [FromQuery] int limit = 10, CancellationToken cancellationToken = default)
        {
            var list = await _recommendationService.GetRecommendationsForUserAsync(_caller.RequireUserId(), limit, cancellationToken);
            return Ok(list);
        }

        [HttpPost]
        [Authorize(Roles = RoleDefinition.Administrator)]
        public override async Task<ActionResult<TrainingResponse>> InsertAsync([FromBody] TrainingInsertRequest request)
            => await base.InsertAsync(request);

        [HttpPut("{id:int}")]
        [Authorize(Roles = RoleDefinition.Administrator)]
        public override async Task<ActionResult<TrainingResponse>> UpdateAsync(int id, [FromBody] TrainingUpdateRequest request)
            => await base.UpdateAsync(id, request);

        [HttpDelete("{id:int}")]
        [Authorize(Roles = RoleDefinition.Administrator)]
        public override async Task<ActionResult> DeleteAsync(int id)
            => await base.DeleteAsync(id);
    }
}
