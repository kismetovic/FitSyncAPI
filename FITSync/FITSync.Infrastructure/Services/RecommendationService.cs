using FITSync.Contracts.Trainings;
using FITSync.Infrastructure.Repositories.Interfaces;
using FITSync.Infrastructure.Services.Interfaces;

namespace FITSync.Infrastructure.Services;

public class RecommendationService : IRecommendationService
{
    private readonly IReservationRepository _reservationRepository;
    private readonly IReviewRepository _reviewRepository;
    private readonly ITrainingRepository _trainingRepository;
    private readonly ITrainingService _trainingService;

    public RecommendationService(
        IReservationRepository reservationRepository,
        IReviewRepository reviewRepository,
        ITrainingRepository trainingRepository,
        ITrainingService trainingService)
    {
        _reservationRepository = reservationRepository;
        _reviewRepository = reviewRepository;
        _trainingRepository = trainingRepository;
        _trainingService = trainingService;
    }

    public async Task<List<TrainingResponse>> GetRecommendationsForUserAsync(int userId, int limit = 10, CancellationToken cancellationToken = default)
    {
        var reservations = await _reservationRepository.GetByUserIdAsync(userId, cancellationToken);
        var reviews = await _reviewRepository.GetByUserIdAsync(userId, cancellationToken);

        var doneTrainingIds = reservations.Select(r => r.TrainingId)
            .Concat(reviews.Select(r => r.TrainingId))
            .Distinct()
            .ToHashSet();

        var preferredTypeIds = reservations.Select(r => r.Training.TrainingTypeId)
            .Concat(reviews.Select(r => r.Training.TrainingTypeId))
            .GroupBy(id => id)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .ToList();

        var candidateSet = new HashSet<int>();
        var contentBasedIds = new List<int>();

        if (preferredTypeIds.Count > 0)
        {
            foreach (var typeId in preferredTypeIds.Take(5))
            {
                var byType = await _trainingRepository.GetByTrainingTypeIdAsync(typeId, cancellationToken);
                foreach (var t in byType.Where(t => !doneTrainingIds.Contains(t.Id)))
                {
                    if (candidateSet.Add(t.Id))
                        contentBasedIds.Add(t.Id);
                }
            }
        }

        var collaborativeIds = new List<int>();
        if (doneTrainingIds.Count > 0)
        {
            var allReservations = await _reservationRepository.GetAsync();
            var similarUserIds = allReservations
                .Where(r => r.UserId != userId && doneTrainingIds.Contains(r.TrainingId))
                .Select(r => r.UserId)
                .GroupBy(uid => uid)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => g.Key)
                .ToHashSet();

            var similarUsersTrainings = allReservations
                .Where(r => similarUserIds.Contains(r.UserId) && !doneTrainingIds.Contains(r.TrainingId))
                .GroupBy(r => r.TrainingId)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .ToList();

            foreach (var tid in similarUsersTrainings)
            {
                if (candidateSet.Add(tid))
                    collaborativeIds.Add(tid);
            }
        }

        var merged = new List<int>();
        int ci = 0, coi = 0;
        while (merged.Count < limit * 2 && (ci < contentBasedIds.Count || coi < collaborativeIds.Count))
        {
            if (ci < contentBasedIds.Count) merged.Add(contentBasedIds[ci++]);
            if (coi < collaborativeIds.Count && merged.Count < limit * 2) merged.Add(collaborativeIds[coi++]);
        }

        if (merged.Count < limit)
        {
            var all = await _trainingRepository.GetAsync();
            foreach (var t in all.Where(t => !doneTrainingIds.Contains(t.Id) && !candidateSet.Contains(t.Id)))
            {
                merged.Add(t.Id);
                if (merged.Count >= limit * 2) break;
            }
        }

        var finalIds = merged.Distinct().Take(limit).ToList();
        if (finalIds.Count == 0)
            return new List<TrainingResponse>();

        return await _trainingService.GetByIdsAsync(finalIds, cancellationToken);
    }
}
