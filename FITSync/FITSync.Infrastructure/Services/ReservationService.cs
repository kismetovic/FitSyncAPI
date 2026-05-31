using AutoMapper;
using FITSync.Contracts.Reservations;
using FITSync.Domain.Entities;
using FITSync.Domain.Enums;
using FITSync.Infrastructure.Context;
using FITSync.Infrastructure.Repositories.Interfaces;
using FITSync.Infrastructure.Services.Interfaces;

namespace FITSync.Infrastructure.Services
{
    public class ReservationService : BaseCRUDService<Reservation, ReservationResponse, ReservationInsertRequest, ReservationUpdateRequest>, IReservationService
    {
        private readonly IReservationRepository _reservationRepository;
        private readonly FitSyncDbContext _context;
        private readonly IEmailNotificationService _emailNotificationService;
        private readonly INotificationRepository _notificationRepository;

        public ReservationService(
            IReservationRepository repository,
            IMapper mapper,
            FitSyncDbContext context,
            IEmailNotificationService emailNotificationService,
            INotificationRepository notificationRepository)
            : base(repository, mapper)
        {
            _reservationRepository = repository;
            _context = context;
            _emailNotificationService = emailNotificationService;
            _notificationRepository = notificationRepository;
        }

        public override async Task BeforeInsert(Reservation entity, ReservationInsertRequest request)
        {
            if (request.AdditionalServiceIds != null && request.AdditionalServiceIds.Count > 0)
            {
                foreach (var additionalServiceId in request.AdditionalServiceIds)
                {
                    _context.ReservationServices.Add(new Domain.Entities.ReservationService
                    {
                        ReservationId = entity.Id,
                        AdditionalServiceId = additionalServiceId
                    });
                }
                await _context.SaveChangesAsync();
            }
        }

        public override async Task BeforeImageUpdate(Reservation entity, ReservationUpdateRequest request)
        {
            entity.ReservationServices = entity.ReservationServices ?? new List<Domain.Entities.ReservationService>();
            entity.ReservationServices.Clear();
            if (request.AdditionalServiceIds != null && request.AdditionalServiceIds.Count > 0)
            {
                foreach (var additionalServiceId in request.AdditionalServiceIds)
                {
                    entity.ReservationServices.Add(new Domain.Entities.ReservationService
                    {
                        ReservationId = entity.Id,
                        AdditionalServiceId = additionalServiceId
                    });
                }
            }
            await Task.CompletedTask;
        }

        public override async Task<ReservationResponse> InsertAsync(ReservationInsertRequest request)
        {
            var existing = await _reservationRepository.GetByUserIdAsync(request.UserId);
            var conflict = existing.Any(r =>
                r.Status != ReservationStatus.Cancelled &&
                r.Training != null &&
                Math.Abs((r.ReservationDate - request.ReservationDate).TotalMinutes) < r.Training.DurationMinutes);

            if (conflict)
            {
                if (request.Status != ReservationStatus.PendingApproval)
                    throw new InvalidOperationException(
                        "TIME_CONFLICT: You already have a reservation at this time. " +
                        "Enable 'outside availability' to request an exception pending trainer approval.");

                request.Status = ReservationStatus.PendingApproval;
            }

            var response = await base.InsertAsync(request);
            try
            {
                var reservation = await _reservationRepository.GetByIdAsync(response.Id);
                if (reservation?.User != null && reservation.Training != null)
                {
                    var email = reservation.User.Email ?? "";
                    var userName = reservation.User.Name ?? reservation.User.UserName ?? email;
                    await _emailNotificationService.SendReservationConfirmationAsync(
                        email, userName, reservation.ReservationDate, reservation.Training.Name);
                }
            }
            catch {  }
            return response;
        }

        public async Task<ReservationResponse?> ApproveAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _reservationRepository.GetByIdAsync(id);
            if (entity == null) return null;
            entity.Status = ReservationStatus.Approved;
            await _reservationRepository.UpdateAsync(entity);

            try
            {
                await _notificationRepository.InsertAsync(new Notification
                {
                    UserId = entity.UserId,
                    Title = "Rezervacija odobrena",
                    Message = $"Vaša rezervacija za trening \"{entity.Training?.Name ?? "trening"}\" " +
                              $"zakazana za {entity.ReservationDate:dd.MM.yyyy HH:mm} je odobrena.",
                    IsRead = false
                });
            }
            catch {  }

            return _mapper.Map<ReservationResponse>(entity);
        }

        public async Task<List<ReservationResponse>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            var entities = await _reservationRepository.GetByUserIdAsync(userId, cancellationToken);
            return _mapper.Map<List<ReservationResponse>>(entities);
        }

        public async Task<List<ReservationResponse>> GetByTrainingIdAsync(int trainingId, CancellationToken cancellationToken = default)
        {
            var entities = await _reservationRepository.GetByTrainingIdAsync(trainingId, cancellationToken);
            return _mapper.Map<List<ReservationResponse>>(entities);
        }
    }
}
