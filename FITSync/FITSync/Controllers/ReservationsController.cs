using FITSync.Contracts.Reservations;
using FITSync.Infrastructure.Helpers;
using FITSync.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FITSync.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReservationsController : BaseCRUDController<ReservationResponse, ReservationInsertRequest, ReservationUpdateRequest>
    {
        private readonly IReservationService _reservationService;
        private readonly ICaller _caller;

        public ReservationsController(IReservationService service, ICaller caller) : base(service)
        {
            _reservationService = service;
            _caller = caller;
        }

        [HttpGet("mine")]
        [HttpGet("my")]
        public async Task<ActionResult<List<ReservationResponse>>> GetMine(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_caller.UserId))
                return Unauthorized();
            var list = await _reservationService.GetByUserIdAsync(int.Parse(_caller.UserId), cancellationToken);
            return Ok(list);
        }

        [HttpPost]
        public override async Task<ActionResult<ReservationResponse>> InsertAsync([FromBody] ReservationInsertRequest request)
        {
            if (!_caller.IsAuthenticated) return Unauthorized();
            if (request.UserId == 0 && !string.IsNullOrEmpty(_caller.UserId))
                request.UserId = int.Parse(_caller.UserId);
            try
            {
                return await base.InsertAsync(request);
            }
            catch (InvalidOperationException ex) when (ex.Message.StartsWith("TIME_CONFLICT"))
            {
                return Conflict(new { error = "TIME_CONFLICT", message = ex.Message.Replace("TIME_CONFLICT: ", "") });
            }
        }

        [HttpGet("by-user/{userId}")]
        public async Task<ActionResult<List<ReservationResponse>>> GetByUserId(int userId, CancellationToken cancellationToken = default)
        {
            if (!_caller.IsAuthenticated) return Unauthorized();
            if (_caller.UserId != userId.ToString() && !User.IsInRole("Administrator"))
                return Forbid();
            var list = await _reservationService.GetByUserIdAsync(userId, cancellationToken);
            return Ok(list);
        }

        [HttpGet("by-training/{trainingId}")]
        public async Task<ActionResult<List<ReservationResponse>>> GetByTrainingId(int trainingId, CancellationToken cancellationToken = default)
        {
            var list = await _reservationService.GetByTrainingIdAsync(trainingId, cancellationToken);
            return Ok(list);
        }

        [HttpPatch("{id}/approve")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<ReservationResponse>> Approve(int id, CancellationToken cancellationToken = default)
        {
            var reservation = await _reservationService.ApproveAsync(id, cancellationToken);
            if (reservation == null) return NotFound();
            return Ok(reservation);
        }
    }
}
