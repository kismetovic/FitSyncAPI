using FITSync.Contracts.Trainings;

namespace FITSync.Infrastructure.Services.Interfaces;

public interface IRecommendationService
{
    Task<List<RecommendedTrainingResponse>> GetRecommendationsForUserAsync(int userId, int limit = 10, CancellationToken cancellationToken = default);
}
