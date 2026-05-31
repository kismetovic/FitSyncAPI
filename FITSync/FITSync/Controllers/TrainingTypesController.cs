using FITSync.Contracts.TrainingTypes;
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

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public override async Task<ActionResult<TrainingTypeResponse>> InsertAsync([FromBody] TrainingTypeInsertRequest request)
            => await base.InsertAsync(request);

        [HttpPut("{id}")]
        [Authorize(Roles = "Administrator")]
        public override async Task<ActionResult<TrainingTypeResponse>> UpdateAsync(int id, [FromBody] TrainingTypeUpdateRequest request)
            => await base.UpdateAsync(id, request);

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrator")]
        public override async Task<ActionResult> DeleteAsync(int id)
            => await base.DeleteAsync(id);
    }
}
