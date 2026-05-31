using AutoMapper;
using FITSync.Contracts.AdditionalServices;
using FITSync.Domain.Entities;
using FITSync.Infrastructure.Repositories.Interfaces;
using FITSync.Infrastructure.Services.Interfaces;

namespace FITSync.Infrastructure.Services
{
    public class AdditionalServiceService : BaseCRUDService<AdditionalService, AdditionalServiceResponse, AdditionalServiceInsertRequest, AdditionalServiceUpdateRequest>, IAdditionalServiceService
    {
        public AdditionalServiceService(IAdditionalServiceRepository repository, IMapper mapper)
            : base(repository, mapper)
        {
        }
    }
}
