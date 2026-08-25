using FITSync.Contracts.Common;
using FITSync.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FITSync.WebAPI.Controllers
{
    /// <summary>
    /// Generic CRUD scaffold. It deliberately carries no role attributes on its actions:
    /// every concrete controller overrides each action and states its own authorisation
    /// rule, so the access policy for a resource is readable in one place rather than
    /// inherited implicitly.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public abstract class BaseCRUDController<TModelDTO, TInsert, TUpdate> : ControllerBase
        where TModelDTO : class
    {
        protected readonly IBaseCRUDService<TModelDTO, TInsert, TUpdate> _service;

        protected BaseCRUDController(IBaseCRUDService<TModelDTO, TInsert, TUpdate> service)
        {
            _service = service;
        }

        [HttpGet]
        public virtual async Task<ActionResult<List<TModelDTO>>> GetAsync()
        {
            var list = await _service.GetAsync();
            return Ok(list);
        }

        [HttpGet("{id:int}")]
        public virtual async Task<ActionResult<TModelDTO>> GetByIdAsync(int id)
        {
            var item = await _service.GetByIdAsync(id);
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

        [HttpPut("{id:int}")]
        public virtual async Task<ActionResult<TModelDTO>> UpdateAsync(int id, [FromBody] TUpdate request)
        {
            var result = await _service.UpdateAsync(id, request);
            if (result == null)
            {
                return NotFound();
            }
            return Ok(result);
        }

        [HttpDelete("{id:int}")]
        public virtual async Task<ActionResult> DeleteAsync(int id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result)
            {
                return BadRequest(new { error = "DELETE_FAILED", message = "Could not delete the entity." });
            }
            return Ok(new { message = "Entity deleted successfully." });
        }

        /// <summary>Applies paging over the generic list read when the service supports it.</summary>
        protected static PagedResult<T> Page<T>(List<T> items, PagedRequest paging)
            => PagedResult<T>.Create(
                items.Skip(paging.Skip).Take(paging.Take).ToList(),
                paging.Page,
                paging.PageSize,
                items.Count);
    }
}
