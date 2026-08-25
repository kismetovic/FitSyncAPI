using FITSync.Contracts.Trainers;

namespace FITSync.Infrastructure.Services.Interfaces
{
    public interface ITrainerService : IBaseCRUDService<TrainerResponse, TrainerInsertRequest, TrainerUpdateRequest>
    {
        Task<List<TrainerAvailabilityResponse>> GetAvailabilityAsync(int trainerId, CancellationToken cancellationToken = default);
        Task<TrainerAvailabilityResponse?> AddAvailabilityAsync(TrainerAvailabilityRequest request, CancellationToken cancellationToken = default);
        Task<bool> RemoveAvailabilityAsync(int availabilityId, CancellationToken cancellationToken = default);

        Task<AvailabilityCheckResult> CheckAvailabilityAsync(int? trainerId, DateTime start, DateTime end, CancellationToken cancellationToken = default);
    }

    public class AvailabilityCheckResult
    {
        public bool IsWithinAvailability { get; set; } = true;
        public decimal Surcharge { get; set; }
        public string? TrainerName { get; set; }
    }
}
