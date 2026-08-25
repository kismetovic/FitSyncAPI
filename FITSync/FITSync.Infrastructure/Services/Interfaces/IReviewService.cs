using FITSync.Contracts.Common;
using FITSync.Contracts.Reviews;

namespace FITSync.Infrastructure.Services.Interfaces
{
    public interface IReviewService : IBaseCRUDService<ReviewResponse, ReviewInsertRequest, ReviewUpdateRequest>
    {
        Task<PagedResult<ReviewResponse>> SearchAsync(int? trainingId, int? userId, string? searchTerm, PagedRequest paging, CancellationToken cancellationToken = default);
        Task<List<ReviewResponse>> GetByTrainingIdAsync(int trainingId, CancellationToken cancellationToken = default);
        Task<List<ReviewResponse>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);

        Task<ReviewResponse> CreateForUserAsync(int authorUserId, ReviewInsertRequest request, CancellationToken cancellationToken = default);

        Task<bool> IsOwnedByAsync(int reviewId, int userId, CancellationToken cancellationToken = default);
    }
}
