using FITSync.Domain.Entities;

namespace FITSync.Infrastructure.Repositories.Interfaces
{
    public interface IReservationRepository : IBaseRepository<Reservation>
    {
        Task<List<Reservation>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
        Task<List<Reservation>> GetByTrainingIdAsync(int trainingId, CancellationToken cancellationToken = default);
    }
}
