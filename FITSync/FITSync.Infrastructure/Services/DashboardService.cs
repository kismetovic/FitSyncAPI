using FITSync.Contracts.Dashboard;
using FITSync.Domain.Enums;
using FITSync.Infrastructure.Repositories.Interfaces;
using FITSync.Infrastructure.Services.Interfaces;

namespace FITSync.Infrastructure.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITrainingRepository _trainingRepository;
        private readonly IReservationRepository _reservationRepository;
        private readonly IPaymentRepository _paymentRepository;

        public DashboardService(
            IUserRepository userRepository,
            ITrainingRepository trainingRepository,
            IReservationRepository reservationRepository,
            IPaymentRepository paymentRepository)
        {
            _userRepository = userRepository;
            _trainingRepository = trainingRepository;
            _reservationRepository = reservationRepository;
            _paymentRepository = paymentRepository;
        }

        public async Task<DashboardStatsResponse> GetStatsAsync(CancellationToken cancellationToken = default)
        {
            var users = await _userRepository.GetAsync();
            var trainings = await _trainingRepository.GetAsync();
            var reservations = await _reservationRepository.GetAsync();
            var payments = await _paymentRepository.GetAsync();

            return new DashboardStatsResponse
            {
                TotalUsers = users.Count,
                TotalTrainings = trainings.Count,
                TotalReservations = reservations.Count,
                TotalRevenue = payments.Sum(p => p.Amount)
            };
        }

        public async Task<List<DashboardTrainingStatsResponse>> GetTrainingStatsAsync(CancellationToken cancellationToken = default)
        {
            var trainings = await _trainingRepository.GetAsync();
            var now = DateTime.UtcNow;
            var result = new List<DashboardTrainingStatsResponse>();
            foreach (var t in trainings)
            {
                var reservations = await _reservationRepository.GetByTrainingIdAsync(t.Id, cancellationToken);
                var futureReservations = reservations.Where(r => r.ReservationDate >= now && r.Status != ReservationStatus.Cancelled).OrderBy(r => r.ReservationDate).ToList();
                result.Add(new DashboardTrainingStatsResponse
                {
                    TrainingId = t.Id,
                    TrainingName = t.Name,
                    ReservationsCount = reservations.Count,
                    NextTerm = futureReservations.FirstOrDefault()?.ReservationDate
                });
            }
            return result;
        }
    }
}
