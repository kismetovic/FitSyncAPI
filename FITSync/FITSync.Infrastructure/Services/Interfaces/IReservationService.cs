using FITSync.Contracts.Common;
using FITSync.Contracts.Reservations;

namespace FITSync.Infrastructure.Services.Interfaces
{
    public interface IReservationService : IBaseCRUDService<ReservationResponse, ReservationInsertRequest, ReservationUpdateRequest>
    {
        Task<PagedResult<ReservationResponse>> SearchAsync(ReservationSearchRequest request, CancellationToken cancellationToken = default);
        Task<List<ReservationResponse>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
        Task<List<ReservationResponse>> GetByTrainingIdAsync(int trainingId, CancellationToken cancellationToken = default);

        Task<ReservationResponse> CreateForUserAsync(int ownerUserId, ReservationInsertRequest request, CancellationToken cancellationToken = default);

        Task<ReservationResponse?> ApproveAsync(int id, int actingUserId, CancellationToken cancellationToken = default);
        Task<ReservationResponse?> CancelAsync(int id, int actingUserId, bool actingAsStaff, string reason, CancellationToken cancellationToken = default);
        Task<ReservationResponse?> CompleteAsync(int id, int actingUserId, string? note, CancellationToken cancellationToken = default);

        Task<bool> IsOwnedByAsync(int reservationId, int userId, CancellationToken cancellationToken = default);
    }
}
