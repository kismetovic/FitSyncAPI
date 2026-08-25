using FITSync.Domain.Entities;

namespace FITSync.Infrastructure.Repositories.Interfaces
{
    public interface ITrainerRepository : IBaseRepository<Trainer>
    {
        Task<Trainer?> GetWithAvailabilityAsync(int trainerId, CancellationToken cancellationToken = default);
        Task<List<TrainerAvailability>> GetAvailabilityAsync(int trainerId, CancellationToken cancellationToken = default);
        Task<TrainerAvailability> AddAvailabilityAsync(TrainerAvailability availability, CancellationToken cancellationToken = default);
        Task<bool> RemoveAvailabilityAsync(int availabilityId, CancellationToken cancellationToken = default);
    }
}
