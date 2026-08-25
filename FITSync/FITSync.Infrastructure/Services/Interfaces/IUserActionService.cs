using FITSync.Domain.Enums;

namespace FITSync.Infrastructure.Services.Interfaces
{
    public interface IUserActionService
    {
        Task LogActionAsync(int userId, UserActionType actionType, int? trainingId = null, int? trainingTypeId = null, string? details = null, CancellationToken cancellationToken = default);
        Task LogTrainingViewAsync(int userId, int trainingId, CancellationToken cancellationToken = default);
        Task LogSearchAsync(int userId, string term, int? trainingTypeId = null, CancellationToken cancellationToken = default);
    }
}
