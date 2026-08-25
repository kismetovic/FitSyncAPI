using AutoMapper;
using FITSync.Contracts.Trainers;
using FITSync.Domain.Entities;
using FITSync.Infrastructure.Exceptions;
using FITSync.Infrastructure.Repositories.Interfaces;
using FITSync.Infrastructure.Services.Interfaces;

namespace FITSync.Infrastructure.Services
{
    public class TrainerService : BaseCRUDService<Trainer, TrainerResponse, TrainerInsertRequest, TrainerUpdateRequest>, ITrainerService
    {
        /// <summary>Applied when a trainer has no surcharge of their own configured.</summary>
        private const decimal DefaultOutsideAvailabilitySurcharge = 10.00m;

        private readonly ITrainerRepository _trainerRepository;

        public TrainerService(ITrainerRepository repository, IMapper mapper)
            : base(repository, mapper)
        {
            _trainerRepository = repository;
        }

        public async Task<List<TrainerAvailabilityResponse>> GetAvailabilityAsync(int trainerId, CancellationToken cancellationToken = default)
        {
            var slots = await _trainerRepository.GetAvailabilityAsync(trainerId, cancellationToken);
            return _mapper.Map<List<TrainerAvailabilityResponse>>(slots);
        }

        public async Task<TrainerAvailabilityResponse?> AddAvailabilityAsync(TrainerAvailabilityRequest request, CancellationToken cancellationToken = default)
        {
            var trainer = await _trainerRepository.GetByIdAsync(request.TrainerId);
            if (trainer == null)
                throw new NotFoundException("Trainer not found.");

            var existing = await _trainerRepository.GetAvailabilityAsync(request.TrainerId, cancellationToken);
            var overlaps = existing.Any(a =>
                a.DayOfWeek == request.DayOfWeek &&
                request.StartTime < a.EndTime &&
                a.StartTime < request.EndTime);

            if (overlaps)
                throw new BusinessRuleException("AVAILABILITY_OVERLAP", "This window overlaps an availability slot the trainer already has.");

            var created = await _trainerRepository.AddAvailabilityAsync(new TrainerAvailability
            {
                TrainerId = request.TrainerId,
                DayOfWeek = request.DayOfWeek,
                StartTime = request.StartTime,
                EndTime = request.EndTime
            }, cancellationToken);

            return _mapper.Map<TrainerAvailabilityResponse>(created);
        }

        public Task<bool> RemoveAvailabilityAsync(int availabilityId, CancellationToken cancellationToken = default)
            => _trainerRepository.RemoveAvailabilityAsync(availabilityId, cancellationToken);

        /// <summary>
        /// The whole session has to fit inside one declared window. A training with no
        /// assigned trainer, or a trainer who has declared no hours at all, is treated as
        /// always available so existing data keeps working.
        /// </summary>
        public async Task<AvailabilityCheckResult> CheckAvailabilityAsync(
            int? trainerId,
            DateTime start,
            DateTime end,
            CancellationToken cancellationToken = default)
        {
            if (!trainerId.HasValue)
                return new AvailabilityCheckResult { IsWithinAvailability = true };

            var trainer = await _trainerRepository.GetWithAvailabilityAsync(trainerId.Value, cancellationToken);
            if (trainer == null)
                return new AvailabilityCheckResult { IsWithinAvailability = true };

            var windows = trainer.Availabilities.Where(a => !a.IsDeleted).ToList();
            if (windows.Count == 0)
            {
                return new AvailabilityCheckResult
                {
                    IsWithinAvailability = true,
                    TrainerName = trainer.FullName
                };
            }

            var covered = windows.Any(w => w.Covers(start, end));

            return new AvailabilityCheckResult
            {
                IsWithinAvailability = covered,
                TrainerName = trainer.FullName,
                Surcharge = covered
                    ? 0m
                    : (trainer.OutsideAvailabilitySurcharge > 0
                        ? trainer.OutsideAvailabilitySurcharge
                        : DefaultOutsideAvailabilitySurcharge)
            };
        }
    }
}
