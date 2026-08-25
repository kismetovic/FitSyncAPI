using FITSync.Contracts.Common;
using FITSync.Contracts.Reservations;
using FITSync.Domain.Definitions;
using FITSync.Infrastructure.Helpers;
using FITSync.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FITSync.WebAPI.Controllers
{
    /// <summary>
    /// Access rules, in short: a client sees and acts only on their own reservations;
    /// listing everyone's reservations, editing a schedule and approving are administrator
    /// operations. Nothing here lets a caller set the owner or the status directly.
    /// </summary>
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

        // ------------------------------------------------------------------
        // Client-facing
        // ------------------------------------------------------------------

        [HttpGet("mine")]
        [HttpGet("my")]
        [Authorize]
        public async Task<ActionResult<List<ReservationResponse>>> GetMine(CancellationToken cancellationToken = default)
        {
            var list = await _reservationService.GetByUserIdAsync(_caller.RequireUserId(), cancellationToken);
            return Ok(list);
        }

        /// <summary>
        /// Creates a reservation for the authenticated caller. UserId and Status are not
        /// part of the request model at all, so they cannot be spoofed.
        /// </summary>
        [HttpPost]
        [Authorize]
        public override async Task<ActionResult<ReservationResponse>> InsertAsync([FromBody] ReservationInsertRequest request)
        {
            var created = await _reservationService.CreateForUserAsync(_caller.RequireUserId(), request);
            // Explicit location rather than CreatedAtAction: MVC strips the "Async" suffix
            // from action names, so nameof(GetByIdAsync) does not match a route.
            return Created($"/api/Reservations/{created.Id}", created);
        }

        /// <summary>Owner or administrator only.</summary>
        [HttpGet("{id:int}")]
        [Authorize]
        public override async Task<ActionResult<ReservationResponse>> GetByIdAsync(int id)
        {
            var reservation = await _reservationService.GetByIdAsync(id);
            if (reservation == null) return NotFound();

            if (!_caller.IsAdministrator && reservation.UserId != _caller.RequireUserId())
                return Forbid();

            return Ok(reservation);
        }

        /// <summary>
        /// Cancels a reservation instead of deleting it: the row stays, with the reason,
        /// who cancelled it and when. The other party is notified.
        /// </summary>
        [HttpPatch("{id:int}/cancel")]
        [Authorize]
        public async Task<ActionResult<ReservationResponse>> Cancel(
            int id,
            [FromBody] ReservationCancelRequest request,
            CancellationToken cancellationToken = default)
        {
            var callerId = _caller.RequireUserId();
            var isAdmin = _caller.IsAdministrator;

            if (!isAdmin && !await _reservationService.IsOwnedByAsync(id, callerId, cancellationToken))
                return Forbid();

            var result = await _reservationService.CancelAsync(id, callerId, isAdmin, request.Reason, cancellationToken);
            return result == null ? NotFound() : Ok(result);
        }

        // ------------------------------------------------------------------
        // Administrative
        // ------------------------------------------------------------------

        /// <summary>Paged listing of every reservation. Administrators only.</summary>
        [HttpGet]
        [Authorize(Roles = RoleDefinition.Administrator)]
        public override async Task<ActionResult<List<ReservationResponse>>> GetAsync()
        {
            var result = await _reservationService.SearchAsync(new ReservationSearchRequest(), default);
            return Ok(result.Items);
        }

        [HttpGet("search")]
        [Authorize(Roles = RoleDefinition.Administrator)]
        public async Task<ActionResult<PagedResult<ReservationResponse>>> Search(
            [FromQuery] ReservationSearchRequest request, CancellationToken cancellationToken = default)
        {
            var result = await _reservationService.SearchAsync(request ?? new ReservationSearchRequest(), cancellationToken);
            return Ok(result);
        }

        [HttpGet("by-user/{userId:int}")]
        [Authorize]
        public async Task<ActionResult<List<ReservationResponse>>> GetByUserId(int userId, CancellationToken cancellationToken = default)
        {
            if (!_caller.IsAdministrator && _caller.RequireUserId() != userId)
                return Forbid();

            var list = await _reservationService.GetByUserIdAsync(userId, cancellationToken);
            return Ok(list);
        }

        /// <summary>
        /// Everyone booked onto a training. This exposes other clients' bookings, so it is
        /// restricted to administrators; the mobile capacity display uses the aggregate
        /// availability endpoint instead.
        /// </summary>
        [HttpGet("by-training/{trainingId:int}")]
        [Authorize(Roles = RoleDefinition.Administrator)]
        public async Task<ActionResult<List<ReservationResponse>>> GetByTrainingId(int trainingId, CancellationToken cancellationToken = default)
        {
            var list = await _reservationService.GetByTrainingIdAsync(trainingId, cancellationToken);
            return Ok(list);
        }

        /// <summary>
        /// Free seats per day for a training, without revealing who booked them. This is
        /// what the mobile calendar needs, and it is safe for any authenticated client.
        /// </summary>
        [HttpGet("availability/{trainingId:int}")]
        [Authorize]
        public async Task<ActionResult<List<TrainingSlotAvailabilityResponse>>> GetAvailability(
            int trainingId,
            [FromQuery] int days = 14,
            CancellationToken cancellationToken = default)
        {
            if (days is < 1 or > 60) days = 14;

            var reservations = await _reservationService.GetByTrainingIdAsync(trainingId, cancellationToken);
            var today = DateTime.UtcNow.Date;

            var counts = reservations
                .Where(r => r.Status != Domain.Enums.ReservationStatus.Cancelled)
                .GroupBy(r => r.ReservationDate.Date)
                .ToDictionary(g => g.Key, g => g.Count());

            var capacity = reservations.FirstOrDefault()?.Training?.MaxCapacity ?? 0;

            var result = Enumerable.Range(0, days)
                .Select(offset =>
                {
                    var day = today.AddDays(offset);
                    var taken = counts.GetValueOrDefault(day);
                    return new TrainingSlotAvailabilityResponse
                    {
                        Date = day,
                        BookedCount = taken,
                        MaxCapacity = capacity,
                        FreeSlots = Math.Max(0, capacity - taken)
                    };
                })
                .ToList();

            return Ok(result);
        }

        [HttpPatch("{id:int}/approve")]
        [Authorize(Roles = RoleDefinition.Administrator)]
        public async Task<ActionResult<ReservationResponse>> Approve(int id, CancellationToken cancellationToken = default)
        {
            var reservation = await _reservationService.ApproveAsync(id, _caller.RequireUserId(), cancellationToken);
            return reservation == null ? NotFound() : Ok(reservation);
        }

        [HttpPatch("{id:int}/complete")]
        [Authorize(Roles = RoleDefinition.Administrator)]
        public async Task<ActionResult<ReservationResponse>> Complete(
            int id,
            [FromBody] ReservationCompleteRequest? request,
            CancellationToken cancellationToken = default)
        {
            var reservation = await _reservationService.CompleteAsync(id, _caller.RequireUserId(), request?.Note, cancellationToken);
            return reservation == null ? NotFound() : Ok(reservation);
        }

        /// <summary>Reschedules a reservation. Status and ownership are not editable here.</summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = RoleDefinition.Administrator)]
        public override async Task<ActionResult<ReservationResponse>> UpdateAsync(int id, [FromBody] ReservationUpdateRequest request)
            => await base.UpdateAsync(id, request);

        /// <summary>
        /// Not supported on purpose. A reservation is cancelled, never removed, so the
        /// history stays auditable.
        /// </summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = RoleDefinition.Administrator)]
        public override Task<ActionResult> DeleteAsync(int id)
            => Task.FromResult<ActionResult>(StatusCode(StatusCodes.Status405MethodNotAllowed, new
            {
                error = "USE_CANCEL_ENDPOINT",
                message = "Reservations are cancelled, not deleted. Use PATCH /api/Reservations/{id}/cancel with a reason."
            }));
    }

    public class TrainingSlotAvailabilityResponse
    {
        public DateTime Date { get; set; }
        public int BookedCount { get; set; }
        public int MaxCapacity { get; set; }
        public int FreeSlots { get; set; }
    }
}
