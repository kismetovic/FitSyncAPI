using FITSync.Contracts.Reports;
using FITSync.Domain.Definitions;
using FITSync.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FITSync.WebAPI.Controllers
{
    /// <summary>
    /// Data behind the two desktop PDF reports. The desktop app renders these numbers and
    /// computes nothing locally, so a printed report always matches the database.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = RoleDefinition.Administrator)]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        /// <summary>Reservations in a period, with status breakdown and totals.</summary>
        [HttpGet("reservations")]
        public async Task<ActionResult<ReservationReportResponse>> GetReservationReport(
            [FromQuery] ReservationReportRequest request, CancellationToken cancellationToken = default)
            => Ok(await _reportService.GetReservationReportAsync(request, cancellationToken));

        /// <summary>Captured revenue per training in a period, with a provider breakdown.</summary>
        [HttpGet("revenue")]
        public async Task<ActionResult<RevenueReportResponse>> GetRevenueReport(
            [FromQuery] RevenueReportRequest request, CancellationToken cancellationToken = default)
            => Ok(await _reportService.GetRevenueReportAsync(request, cancellationToken));
    }
}
