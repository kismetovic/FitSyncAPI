using FITSync.Contracts.Reviews;

namespace FITSync.Infrastructure.Services.Interfaces
{
    public interface IReviewService : IBaseCRUDService<ReviewResponse, ReviewInsertRequest, ReviewUpdateRequest>
    {
        Task<List<ReviewResponse>> GetByTrainingIdAsync(int trainingId, CancellationToken cancellationToken = default);
        Task<List<ReviewResponse>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    }
}
