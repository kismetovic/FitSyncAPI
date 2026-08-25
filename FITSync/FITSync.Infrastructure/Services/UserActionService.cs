using FITSync.Domain.Entities;
using FITSync.Domain.Enums;
using FITSync.Infrastructure.Repositories.Interfaces;
using FITSync.Infrastructure.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FITSync.Infrastructure.Services
{
    /// <summary>
    /// Persists the behavioural signals the recommender scores on. This used to be an
    /// empty stub returning Task.CompletedTask, which meant the recommender had nothing
    /// beyond completed reservations to work with.
    /// </summary>
    public class UserActionService : IUserActionService
    {
        private readonly IUserActionRepository _repository;
        private readonly ITrainingRepository _trainingRepository;
        private readonly ILogger<UserActionService> _logger;

        public UserActionService(
            IUserActionRepository repository,
            ITrainingRepository trainingRepository,
            ILogger<UserActionService> logger)
        {
            _repository = repository;
            _trainingRepository = trainingRepository;
            _logger = logger;
        }

        public async Task LogActionAsync(
            int userId,
            UserActionType actionType,
            int? trainingId = null,
            int? trainingTypeId = null,
            string? details = null,
            CancellationToken cancellationToken = default)
        {
            // Analytics must never take down the business operation that triggered it.
            try
            {
                if (trainingId.HasValue && !trainingTypeId.HasValue)
                {
                    var training = await _trainingRepository.GetByIdAsync(trainingId.Value);
                    trainingTypeId = training?.TrainingTypeId;
                }

                await _repository.InsertAsync(new UserAction
                {
                    UserId = userId,
                    ActionType = actionType,
                    TrainingId = trainingId,
                    TrainingTypeId = trainingTypeId,
                    Details = details,
                    OccurredAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not record user action {ActionType} for user {UserId}.", actionType, userId);
            }
        }

        public Task LogTrainingViewAsync(int userId, int trainingId, CancellationToken cancellationToken = default)
            => LogActionAsync(userId, UserActionType.ViewedTraining, trainingId, null, null, cancellationToken);

        public Task LogSearchAsync(int userId, string term, int? trainingTypeId = null, CancellationToken cancellationToken = default)
            => LogActionAsync(userId, UserActionType.SearchedTraining, null, trainingTypeId, term, cancellationToken);
    }
}
