using FITSync.Contracts.Trainings;

namespace FITSync.Infrastructure.Services.Interfaces
{
    public interface ITrainingService : IBaseCRUDService<TrainingResponse, TrainingInsertRequest, TrainingUpdateRequest>
    {
        Task<List<TrainingResponse>> GetByTrainingTypeIdAsync(int trainingTypeId, CancellationToken cancellationToken = default);
        Task<List<TrainingResponse>> SearchAsync(TrainingSearchRequest request, CancellationToken cancellationToken = default);
        Task<List<TrainingResponse>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);
    }
}
