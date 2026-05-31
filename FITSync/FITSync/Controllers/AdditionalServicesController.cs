using FITSync.Contracts.AdditionalServices;
using FITSync.Infrastructure.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FITSync.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdditionalServicesController : BaseCRUDController<AdditionalServiceResponse, AdditionalServiceInsertRequest, AdditionalServiceUpdateRequest>
    {
        public AdditionalServicesController(IAdditionalServiceService service) : base(service)
        {
        }
    }
}
