using FITSync.Contracts.Dashboard;
using FITSync.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FITSync.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrator")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("stats")]
        public async Task<ActionResult<DashboardStatsResponse>> GetStats(CancellationToken cancellationToken = default)
        {
            var stats = await _dashboardService.GetStatsAsync(cancellationToken);
            return Ok(stats);
        }

        [HttpGet("training-stats")]
        public async Task<ActionResult<List<DashboardTrainingStatsResponse>>> GetTrainingStats(CancellationToken cancellationToken = default)
        {
            var list = await _dashboardService.GetTrainingStatsAsync(cancellationToken);
            return Ok(list);
        }
    }
}
