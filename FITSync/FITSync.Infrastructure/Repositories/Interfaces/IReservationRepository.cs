using FITSync.Domain.Entities;
using FITSync.Domain.Enums;

namespace FITSync.Infrastructure.Repositories.Interfaces
{
    public interface IReservationRepository : IBaseRepository<Reservation>
    {
        Task<List<Reservation>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
        Task<List<Reservation>> GetByTrainingIdAsync(int trainingId, CancellationToken cancellationToken = default);

        Task<(List<Reservation> Items, int TotalCount)> SearchAsync(
            int? userId, int? trainingId, ReservationStatus? status,
            DateTime? fromDate, DateTime? toDate, string? searchTerm,
            int skip, int take, CancellationToken cancellationToken = default);

        Task<List<Reservation>> GetOverlappingForUserAsync(
            int userId, DateTime newStart, DateTime newEnd,
            int? excludeReservationId = null, CancellationToken cancellationToken = default);

        Task<int> CountActiveForSlotAsync(
            int trainingId, DateTime slotStart,
            int? excludeReservationId = null, CancellationToken cancellationToken = default);

        Task<List<Reservation>> GetForReportAsync(
            DateTime from, DateTime to, int? trainingId, CancellationToken cancellationToken = default);

        Task<List<Reservation>> GetUnpaidByUserIdAsync(int userId, CancellationToken cancellationToken = default);

        Task<Dictionary<int, (int Total, DateTime? NextTerm)>> GetStatsByTrainingAsync(
            DateTime now, CancellationToken cancellationToken = default);

        Task<List<(int UserId, int TrainingId)>> GetPeerReservationsAsync(
            int excludeUserId, IEnumerable<int> trainingIds, CancellationToken cancellationToken = default);

        Task<List<Reservation>> GetCompletableAsync(DateTime now, CancellationToken cancellationToken = default);
    }
}
