using AutoMapper;
using FITSync.Contracts.TrainingTypes;
using FITSync.Domain.Entities;
using FITSync.Infrastructure.Repositories.Interfaces;
using FITSync.Infrastructure.Services.Interfaces;

namespace FITSync.Infrastructure.Services
{
    public class TrainingTypeService : BaseCRUDService<TrainingType, TrainingTypeResponse, TrainingTypeInsertRequest, TrainingTypeUpdateRequest>, ITrainingTypeService
    {
        public TrainingTypeService(ITrainingTypeRepository repository, IMapper mapper)
            : base(repository, mapper)
        {
        }
    }
}
