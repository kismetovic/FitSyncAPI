using FITSync.Domain.Entities;

namespace FITSync.Infrastructure.Repositories.Interfaces
{
    public interface IReservationStatusHistoryRepository : IBaseRepository<ReservationStatusHistory>
    {
        Task<List<ReservationStatusHistory>> GetByReservationIdAsync(int reservationId, CancellationToken cancellationToken = default);
    }
}
