using FITSync.Contracts.TrainingTypes;
using FITSync.Domain.Definitions;
using FITSync.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FITSync.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TrainingTypesController : BaseCRUDController<TrainingTypeResponse, TrainingTypeInsertRequest, TrainingTypeUpdateRequest>
    {
        public TrainingTypesController(ITrainingTypeService service) : base(service)
        {
        }

        [HttpGet]
        [Authorize]
        public override async Task<ActionResult<List<TrainingTypeResponse>>> GetAsync()
            => await base.GetAsync();

        [HttpGet("{id:int}")]
        [Authorize]
        public override async Task<ActionResult<TrainingTypeResponse>> GetByIdAsync(int id)
            => await base.GetByIdAsync(id);

        [HttpPost]
        [Authorize(Roles = RoleDefinition.Administrator)]
        public override async Task<ActionResult<TrainingTypeResponse>> InsertAsync([FromBody] TrainingTypeInsertRequest request)
            => await base.InsertAsync(request);

        [HttpPut("{id:int}")]
        [Authorize(Roles = RoleDefinition.Administrator)]
        public override async Task<ActionResult<TrainingTypeResponse>> UpdateAsync(int id, [FromBody] TrainingTypeUpdateRequest request)
            => await base.UpdateAsync(id, request);

        [HttpDelete("{id:int}")]
        [Authorize(Roles = RoleDefinition.Administrator)]
        public override async Task<ActionResult> DeleteAsync(int id)
            => await base.DeleteAsync(id);
    }
}
