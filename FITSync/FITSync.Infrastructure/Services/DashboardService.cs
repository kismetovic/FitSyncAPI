using FITSync.Contracts.Dashboard;
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

        /// <summary>
        /// Four COUNT/SUM queries. The previous version loaded every user, training,
        /// reservation and payment into memory just to count them.
        /// </summary>
        public async Task<DashboardStatsResponse> GetStatsAsync(CancellationToken cancellationToken = default)
        {
            var stats = await _reservationRepository.GetStatsByTrainingAsync(DateTime.UtcNow, cancellationToken);

            return new DashboardStatsResponse
            {
                TotalUsers = await _userRepository.CountAsync(cancellationToken),
                TotalTrainings = await _trainingRepository.CountAsync(cancellationToken),
                TotalReservations = stats.Values.Sum(v => v.Total),
                TotalRevenue = await _paymentRepository.GetTotalCapturedRevenueAsync(cancellationToken)
            };
        }

        /// <summary>
        /// One grouped query for all trainings. The previous version ran a separate
        /// reservation query for every single training.
        /// </summary>
        public async Task<List<DashboardTrainingStatsResponse>> GetTrainingStatsAsync(CancellationToken cancellationToken = default)
        {
            var trainings = await _trainingRepository.GetAsync();
            var stats = await _reservationRepository.GetStatsByTrainingAsync(DateTime.UtcNow, cancellationToken);

            return trainings.Select(t =>
            {
                stats.TryGetValue(t.Id, out var s);
                return new DashboardTrainingStatsResponse
                {
                    TrainingId = t.Id,
                    TrainingName = t.Name,
                    ReservationsCount = s.Total,
                    NextTerm = s.NextTerm
                };
            }).ToList();
        }
    }
}
