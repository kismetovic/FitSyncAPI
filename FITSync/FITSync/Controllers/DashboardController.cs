using FITSync.Contracts.Dashboard;
using FITSync.Domain.Definitions;
using FITSync.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FITSync.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = RoleDefinition.Administrator)]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("stats")]
        public async Task<ActionResult<DashboardStatsResponse>> GetStats(CancellationToken cancellationToken = default)
            => Ok(await _dashboardService.GetStatsAsync(cancellationToken));

        [HttpGet("training-stats")]
        public async Task<ActionResult<List<DashboardTrainingStatsResponse>>> GetTrainingStats(CancellationToken cancellationToken = default)
            => Ok(await _dashboardService.GetTrainingStatsAsync(cancellationToken));
    }
}
