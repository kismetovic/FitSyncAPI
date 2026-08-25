using FITSync.Contracts.Trainers;
using FITSync.Domain.Definitions;
using FITSync.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FITSync.WebAPI.Controllers
{
    /// <summary>
    /// Trainer profiles and their weekly working hours. Clients read them so the booking
    /// screen can show when a slot falls outside a trainer's hours; only administrators
    /// may change them.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TrainersController : BaseCRUDController<TrainerResponse, TrainerInsertRequest, TrainerUpdateRequest>
    {
        private readonly ITrainerService _trainerService;

        public TrainersController(ITrainerService service) : base(service)
        {
            _trainerService = service;
        }

        [HttpGet]
        [Authorize]
        public override async Task<ActionResult<List<TrainerResponse>>> GetAsync()
            => await base.GetAsync();

        [HttpGet("{id:int}")]
        [Authorize]
        public override async Task<ActionResult<TrainerResponse>> GetByIdAsync(int id)
            => await base.GetByIdAsync(id);

        [HttpGet("{id:int}/availability")]
        [Authorize]
        public async Task<ActionResult<List<TrainerAvailabilityResponse>>> GetAvailability(int id, CancellationToken cancellationToken = default)
            => Ok(await _trainerService.GetAvailabilityAsync(id, cancellationToken));

        /// <summary>
        /// Tells the client whether a proposed slot is inside the trainer's hours and, if
        /// not, what the surcharge would be. The same check is re-run when the reservation
        /// is actually created; this endpoint exists so the UI can warn in advance.
        /// </summary>
        [HttpGet("{id:int}/availability/check")]
        [Authorize]
        public async Task<ActionResult<AvailabilityCheckResult>> CheckAvailability(
            int id,
            [FromQuery] DateTime start,
            [FromQuery] int durationMinutes,
            CancellationToken cancellationToken = default)
        {
            if (durationMinutes is < 5 or > 600)
                return BadRequest(new { error = "INVALID_DURATION", message = "Duration must be between 5 and 600 minutes." });

            var result = await _trainerService.CheckAvailabilityAsync(id, start, start.AddMinutes(durationMinutes), cancellationToken);
            return Ok(result);
        }

        [HttpPost("availability")]
        [Authorize(Roles = RoleDefinition.Administrator)]
        public async Task<ActionResult<TrainerAvailabilityResponse>> AddAvailability(
            [FromBody] TrainerAvailabilityRequest request, CancellationToken cancellationToken = default)
        {
            var created = await _trainerService.AddAvailabilityAsync(request, cancellationToken);
            return created == null ? NotFound() : Ok(created);
        }

        [HttpDelete("availability/{availabilityId:int}")]
        [Authorize(Roles = RoleDefinition.Administrator)]
        public async Task<ActionResult> RemoveAvailability(int availabilityId, CancellationToken cancellationToken = default)
        {
            var removed = await _trainerService.RemoveAvailabilityAsync(availabilityId, cancellationToken);
            return removed ? Ok(new { message = "Availability slot removed." }) : NotFound();
        }

        [HttpPost]
        [Authorize(Roles = RoleDefinition.Administrator)]
        public override async Task<ActionResult<TrainerResponse>> InsertAsync([FromBody] TrainerInsertRequest request)
            => await base.InsertAsync(request);

        [HttpPut("{id:int}")]
        [Authorize(Roles = RoleDefinition.Administrator)]
        public override async Task<ActionResult<TrainerResponse>> UpdateAsync(int id, [FromBody] TrainerUpdateRequest request)
            => await base.UpdateAsync(id, request);

        [HttpDelete("{id:int}")]
        [Authorize(Roles = RoleDefinition.Administrator)]
        public override async Task<ActionResult> DeleteAsync(int id)
            => await base.DeleteAsync(id);
    }
}
