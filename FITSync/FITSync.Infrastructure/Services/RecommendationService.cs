using FITSync.Contracts.Trainings;
using FITSync.Domain.Entities;
using FITSync.Infrastructure.Repositories.Interfaces;
using FITSync.Infrastructure.Services.Interfaces;

namespace FITSync.Infrastructure.Services;

/// <summary>
/// Hybrid recommender: a content-based half that scores training types the user has
/// engaged with, and a collaborative half that scores what similar users booked.
/// Both halves contribute to one score, and every result carries the reason it was
/// picked. The scoring weights and the fallback order are documented in docs/RECOMMENDER.md.
/// </summary>
public class RecommendationService : IRecommendationService
{
    // Score weights. Kept here as named constants so the documentation and the code
    // cannot drift apart.
    private const double ContentTypeWeight = 1.0;
    private const double CollaborativeWeight = 0.8;
    private const double RecentViewWeight = 0.5;
    private const double RatingWeight = 0.4;
    private const double PopularityWeight = 0.2;

    private readonly IReservationRepository _reservationRepository;
    private readonly IReviewRepository _reviewRepository;
    private readonly ITrainingRepository _trainingRepository;
    private readonly IUserActionRepository _userActionRepository;

    public RecommendationService(
        IReservationRepository reservationRepository,
        IReviewRepository reviewRepository,
        ITrainingRepository trainingRepository,
        IUserActionRepository userActionRepository)
    {
        _reservationRepository = reservationRepository;
        _reviewRepository = reviewRepository;
        _trainingRepository = trainingRepository;
        _userActionRepository = userActionRepository;
    }

    public async Task<List<RecommendedTrainingResponse>> GetRecommendationsForUserAsync(
        int userId, int limit = 10, CancellationToken cancellationToken = default)
    {
        if (limit <= 0) limit = 10;
        if (limit > 50) limit = 50;

        var reservations = await _reservationRepository.GetByUserIdAsync(userId, cancellationToken);
        var reviews = await _reviewRepository.GetByUserIdAsync(userId, cancellationToken);
        var actionWeights = await _userActionRepository.GetTrainingTypeWeightsAsync(userId, cancellationToken);
        var recentlyViewed = await _userActionRepository.GetRecentlyViewedTrainingIdsAsync(userId, 20, cancellationToken);

        // Trainings the user has already had are not re-recommended.
        var seenTrainingIds = reservations.Select(r => r.TrainingId)
            .Concat(reviews.Select(r => r.TrainingId))
            .ToHashSet();

        var typeAffinity = BuildTypeAffinity(reservations, reviews, actionWeights);

        var candidates = await LoadCandidatesAsync(typeAffinity.Keys, recentlyViewed, seenTrainingIds, cancellationToken);
        if (candidates.Count == 0)
            return new List<RecommendedTrainingResponse>();

        var peerScores = await BuildPeerScoresAsync(userId, seenTrainingIds, cancellationToken);
        var ratingStats = await _reviewRepository.GetStatsByTrainingIdsAsync(candidates.Select(c => c.Id), cancellationToken);
        var maxPeerScore = peerScores.Count == 0 ? 1 : Math.Max(1, peerScores.Values.Max());
        var maxAffinity = typeAffinity.Count == 0 ? 1 : Math.Max(1, typeAffinity.Values.Max());

        var scored = new List<RecommendedTrainingResponse>();

        foreach (var training in candidates)
        {
            var signals = new List<string>();
            double score = 0;
            var strategy = "Popular";

            // --- Content-based: does this training's type match what the user engages with?
            if (typeAffinity.TryGetValue(training.TrainingTypeId, out var affinity) && affinity > 0)
            {
                var normalised = affinity / (double)maxAffinity;
                score += normalised * ContentTypeWeight;
                strategy = "ContentBased";
                signals.Add($"Tip treninga: {training.TrainingType?.Name ?? "isti tip"}");
            }

            // --- Collaborative: did users with similar history book this training?
            if (peerScores.TryGetValue(training.Id, out var peers) && peers > 0)
            {
                var normalised = peers / (double)maxPeerScore;
                var contribution = normalised * CollaborativeWeight;
                score += contribution;
                if (strategy != "ContentBased" || contribution > ContentTypeWeight / 2)
                    strategy = strategy == "ContentBased" ? "ContentBased" : "Collaborative";
                signals.Add($"Rezervisali korisnici sličnih navika ({peers})");
            }

            // --- Behavioural: the user recently looked at this exact training.
            if (recentlyViewed.Contains(training.Id))
            {
                score += RecentViewWeight;
                signals.Add("Nedavno ste pregledali ovaj trening");
            }

            // --- Quality: well-rated trainings are surfaced ahead of unrated ones.
            if (ratingStats.TryGetValue(training.Id, out var stats) && stats.ReviewCount > 0)
            {
                score += (stats.AverageRating / 5.0) * RatingWeight;
                signals.Add($"Prosječna ocjena {stats.AverageRating:0.0} ({stats.ReviewCount})");
            }

            // --- Baseline so a cold-start user still gets a sensible, stable ordering.
            score += PopularityWeight * (1.0 / (1 + training.Id % 7));

            if (score <= PopularityWeight && signals.Count == 0)
            {
                strategy = "Fallback";
                signals.Add("Popularno u teretani");
            }

            scored.Add(new RecommendedTrainingResponse
            {
                Id = training.Id,
                CreatedAt = training.CreatedAt,
                Name = training.Name,
                Description = training.Description,
                Price = training.Price,
                DurationMinutes = training.DurationMinutes,
                MaxCapacity = training.MaxCapacity,
                Difficulty = training.Difficulty,
                TrainingTypeId = training.TrainingTypeId,
                TrainingType = training.TrainingType == null
                    ? null
                    : new TrainingTypeSummaryResponse { Id = training.TrainingType.Id, Name = training.TrainingType.Name },
                TrainerId = training.TrainerId,
                TrainerName = training.Trainer?.FullName,
                AverageRating = ratingStats.TryGetValue(training.Id, out var s) ? s.AverageRating : null,
                ReviewCount = ratingStats.TryGetValue(training.Id, out var s2) ? s2.ReviewCount : 0,
                Score = Math.Round(score, 4),
                Strategy = strategy,
                MatchedSignals = signals,
                Reason = BuildReason(strategy, training, signals)
            });
        }

        return scored
            .OrderByDescending(r => r.Score)
            .ThenByDescending(r => r.AverageRating ?? 0)
            .Take(limit)
            .ToList();
    }

    /// <summary>
    /// Interest per training type, combining explicit history (bookings, reviews) with the
    /// implicit signals recorded in UserActions.
    /// </summary>
    private static Dictionary<int, int> BuildTypeAffinity(
        List<Reservation> reservations,
        List<Review> reviews,
        Dictionary<int, int> actionWeights)
    {
        var affinity = new Dictionary<int, int>(actionWeights);

        foreach (var reservation in reservations)
        {
            var typeId = reservation.Training?.TrainingTypeId;
            if (typeId.HasValue)
                affinity[typeId.Value] = affinity.GetValueOrDefault(typeId.Value) + 4;
        }

        foreach (var review in reviews)
        {
            var typeId = review.Training?.TrainingTypeId;
            if (typeId.HasValue)
                affinity[typeId.Value] = affinity.GetValueOrDefault(typeId.Value) + review.Rating;
        }

        return affinity;
    }

    /// <summary>
    /// Candidate pool, fetched in at most three batched queries: preferred types, recently
    /// viewed trainings, and a general fallback when neither produced enough.
    /// </summary>
    private async Task<List<Training>> LoadCandidatesAsync(
        IEnumerable<int> preferredTypeIds,
        List<int> recentlyViewed,
        HashSet<int> seenTrainingIds,
        CancellationToken cancellationToken)
    {
        var byId = new Dictionary<int, Training>();

        var typeIds = preferredTypeIds.ToList();
        if (typeIds.Count > 0)
        {
            foreach (var training in await _trainingRepository.GetByTrainingTypeIdsAsync(typeIds, cancellationToken))
                byId[training.Id] = training;
        }

        if (recentlyViewed.Count > 0)
        {
            foreach (var training in await _trainingRepository.GetByIdsAsync(recentlyViewed, cancellationToken))
                byId[training.Id] = training;
        }

        if (byId.Count < 20)
        {
            foreach (var training in await _trainingRepository.GetAsync())
                byId.TryAdd(training.Id, training);
        }

        return byId.Values.Where(t => !seenTrainingIds.Contains(t.Id)).ToList();
    }

    /// <summary>
    /// How many peers who share history with this user booked each training. The peer set
    /// is restricted in SQL rather than by loading every reservation in the database.
    /// </summary>
    private async Task<Dictionary<int, int>> BuildPeerScoresAsync(
        int userId, HashSet<int> seenTrainingIds, CancellationToken cancellationToken)
    {
        if (seenTrainingIds.Count == 0)
            return new Dictionary<int, int>();

        var peerRows = await _reservationRepository.GetPeerReservationsAsync(userId, seenTrainingIds, cancellationToken);

        var similarity = peerRows
            .Where(r => seenTrainingIds.Contains(r.TrainingId))
            .GroupBy(r => r.UserId)
            .ToDictionary(g => g.Key, g => g.Count());

        var scores = new Dictionary<int, int>();
        foreach (var row in peerRows)
        {
            if (seenTrainingIds.Contains(row.TrainingId)) continue;
            if (!similarity.TryGetValue(row.UserId, out var weight)) continue;
            scores[row.TrainingId] = scores.GetValueOrDefault(row.TrainingId) + weight;
        }

        return scores;
    }

    /// <summary>The sentence the mobile app shows under a recommended training.</summary>
    private static string BuildReason(string strategy, Training training, List<string> signals)
    {
        var typeName = training.TrainingType?.Name;

        return strategy switch
        {
            "ContentBased" when !string.IsNullOrWhiteSpace(typeName)
                => $"Jer često rezervišete treninge tipa \"{typeName}\".",
            "ContentBased"
                => "Jer odgovara tipovima treninga koje već birate.",
            "Collaborative"
                => "Jer su ga rezervisali korisnici sa sličnim navikama kao Vi.",
            "Popular" when signals.Count > 0
                => $"Preporučeno na osnovu: {string.Join(", ", signals.Take(2)).ToLowerInvariant()}.",
            _
                => "Popularan trening u teretani koji još niste probali."
        };
    }
}
