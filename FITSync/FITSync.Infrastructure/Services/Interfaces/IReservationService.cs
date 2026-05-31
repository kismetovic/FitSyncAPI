using FITSync.Contracts.Reservations;

namespace FITSync.Infrastructure.Services.Interfaces
{
    public interface IReservationService : IBaseCRUDService<ReservationResponse, ReservationInsertRequest, ReservationUpdateRequest>
    {
        Task<List<ReservationResponse>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
        Task<List<ReservationResponse>> GetByTrainingIdAsync(int trainingId, CancellationToken cancellationToken = default);
        Task<ReservationResponse?> ApproveAsync(int id, CancellationToken cancellationToken = default);
    }
}
