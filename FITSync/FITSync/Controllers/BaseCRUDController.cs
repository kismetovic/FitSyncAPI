using FITSync.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FITSync.WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BaseCRUDController<TModelDTO, TInsert, TUpdate> : ControllerBase
        where TModelDTO : class
    {
        protected readonly IBaseCRUDService<TModelDTO, TInsert, TUpdate> _service;

        public BaseCRUDController(IBaseCRUDService<TModelDTO, TInsert, TUpdate> service)
        {
            _service = service;
        }

        [HttpGet]
        public virtual async Task<ActionResult<List<TModelDTO>>> GetAsync()
        {
            var list = await _service.GetAsync();
            return Ok(list);
        }

        [HttpGet("{id}")]
        public virtual ActionResult<TModelDTO> GetByIdAsync(int id)
        {
            var item = _service.GetByIdAsync(id);
            if (item == null)
            {
                return NotFound();
            }
            return Ok(item);
        }

        [HttpPost]
        public virtual async Task<ActionResult<TModelDTO>> InsertAsync([FromBody] TInsert request)
        {
            var result = await _service.InsertAsync(request);
            return Ok(result);
        }

        [HttpPut("{id}")]
        public virtual async Task<ActionResult<TModelDTO>> UpdateAsync(int id, [FromBody] TUpdate request)
        {
            var result = await _service.UpdateAsync(id, request);
            if (result == null)
            {
                return BadRequest("Update failed or entity not found.");
            }
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public virtual async Task<ActionResult> DeleteAsync(int id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result)
            {
                return BadRequest("Could not delete entity.");
            }
            return Ok("Entity deleted successfully.");
        }
    }
}
