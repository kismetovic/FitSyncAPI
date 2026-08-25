using FITSync.Domain.Definitions;
using FITSync.Domain.Entities;
using FITSync.Domain.Enums;
using FITSync.Infrastructure.Context;
using FITSync.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FITSync.Infrastructure.Repositories
{
    public class ReservationRepository : BaseRepository<Reservation>, IReservationRepository
    {
        public ReservationRepository(FitSyncDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Single include set shared by every read. ReservationServices used to be included
        /// only on some paths, so the admin list came back with empty AdditionalServiceIds
        /// while the client list did not; defining it once removes that class of bug.
        /// </summary>
        protected override IQueryable<Reservation> BaseQuery()
        {
            return _dbSet
                .Where(r => !r.IsDeleted)
                .Include(r => r.Training).ThenInclude(t => t.Trainer)
                .Include(r => r.User)
                .Include(r => r.ReservationServices)
                .Include(r => r.Payments);
        }

        public async Task<List<Reservation>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await BaseQuery()
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.ReservationDate)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Reservation>> GetByTrainingIdAsync(int trainingId, CancellationToken cancellationToken = default)
        {
            return await BaseQuery()
                .Where(r => r.TrainingId == trainingId)
                .OrderByDescending(r => r.ReservationDate)
                .ToListAsync(cancellationToken);
        }

        public override async Task<List<Reservation>> GetAsync()
        {
            return await BaseQuery()
                .OrderByDescending(r => r.ReservationDate)
                .ToListAsync();
        }

        public override async Task<Reservation?> GetByIdAsync(int id)
        {
            return await BaseQuery()
                .Include(r => r.StatusHistory)
                .Include(r => r.UserMembership)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<(List<Reservation> Items, int TotalCount)> SearchAsync(
            int? userId,
            int? trainingId,
            ReservationStatus? status,
            DateTime? fromDate,
            DateTime? toDate,
            string? searchTerm,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            var query = BaseQuery();

            if (userId.HasValue)
                query = query.Where(r => r.UserId == userId.Value);
            if (trainingId.HasValue)
                query = query.Where(r => r.TrainingId == trainingId.Value);
            if (status.HasValue)
                query = query.Where(r => r.Status == status.Value);
            if (fromDate.HasValue)
                query = query.Where(r => r.ReservationDate >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(r => r.ReservationDate <= toDate.Value);

            // Matched in SQL rather than after the page has been materialised, so
            // that searching a paged list still hits the whole table exactly once.
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                query = query.Where(r =>
                    (r.User != null &&
                        ((r.User.Name != null && r.User.Name.Contains(term)) ||
                         (r.User.Surname != null && r.User.Surname.Contains(term)) ||
                         (r.User.UserName != null && r.User.UserName.Contains(term)) ||
                         (r.User.Email != null && r.User.Email.Contains(term)))) ||
                    (r.Training != null && r.Training.Name.Contains(term)));
            }

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderByDescending(r => r.ReservationDate)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);

            return (items, total);
        }

        /// <summary>
        /// Reservations of one user that overlap the given window, evaluated in the database.
        /// Overlap is the real interval test: newStart &lt; existingEnd AND existingStart &lt; newEnd.
        /// </summary>
        public async Task<List<Reservation>> GetOverlappingForUserAsync(
            int userId,
            DateTime newStart,
            DateTime newEnd,
            int? excludeReservationId = null,
            CancellationToken cancellationToken = default)
        {
            var query = _dbSet
                .Where(r => !r.IsDeleted
                            && r.UserId == userId
                            && r.Status != ReservationStatus.Cancelled)
                .Include(r => r.Training)
                .AsQueryable();

            if (excludeReservationId.HasValue)
                query = query.Where(r => r.Id != excludeReservationId.Value);

            // EF translates AddMinutes on a DateTime column to DATEADD, so the interval
            // test runs server-side rather than pulling every reservation into memory.
            return await query
                .Where(r => newStart < r.ReservationDate.AddMinutes(r.Training.DurationMinutes)
                            && r.ReservationDate < newEnd)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// How many seats of a training are already taken at an exact slot. Cancelled
        /// reservations do not occupy a seat.
        /// </summary>
        public async Task<int> CountActiveForSlotAsync(
            int trainingId,
            DateTime slotStart,
            int? excludeReservationId = null,
            CancellationToken cancellationToken = default)
        {
            var query = _dbSet
                .Where(r => !r.IsDeleted
                            && r.TrainingId == trainingId
                            && r.ReservationDate == slotStart
                            && r.Status != ReservationStatus.Cancelled);

            if (excludeReservationId.HasValue)
                query = query.Where(r => r.Id != excludeReservationId.Value);

            return await query.CountAsync(cancellationToken);
        }

        public async Task<List<Reservation>> GetForReportAsync(
            DateTime from,
            DateTime to,
            int? trainingId,
            CancellationToken cancellationToken = default)
        {
            var query = _dbSet
                .Where(r => !r.IsDeleted && r.ReservationDate >= from && r.ReservationDate <= to)
                .Include(r => r.Training).ThenInclude(t => t.Trainer)
                .Include(r => r.User)
                .Include(r => r.Payments)
                .AsQueryable();

            if (trainingId.HasValue)
                query = query.Where(r => r.TrainingId == trainingId.Value);

            return await query
                .OrderBy(r => r.ReservationDate)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Unpaid, non-cancelled reservations of a user - the input to the payment reminder.
        /// Replaces the old per-reservation payment lookup loop.
        /// </summary>
        public async Task<List<Reservation>> GetUnpaidByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(r => !r.IsDeleted
                            && r.UserId == userId
                            && r.Status != ReservationStatus.Cancelled
                            && r.Status != ReservationStatus.Completed
                            && !r.Payments.Any(p => !p.IsDeleted && p.Status == PaymentStatus.Captured))
                .Include(r => r.Training)
                .OrderBy(r => r.ReservationDate)
                .ToListAsync(cancellationToken);
        }

        /// <summary>Reservation counts per training, in one grouped query instead of N queries.</summary>
        public async Task<Dictionary<int, (int Total, DateTime? NextTerm)>> GetStatsByTrainingAsync(
            DateTime now,
            CancellationToken cancellationToken = default)
        {
            var rows = await _dbSet
                .Where(r => !r.IsDeleted)
                .GroupBy(r => r.TrainingId)
                .Select(g => new
                {
                    TrainingId = g.Key,
                    Total = g.Count(),
                    NextTerm = g
                        .Where(r => r.ReservationDate >= now && r.Status != ReservationStatus.Cancelled)
                        .Min(r => (DateTime?)r.ReservationDate)
                })
                .ToListAsync(cancellationToken);

            return rows.ToDictionary(x => x.TrainingId, x => (x.Total, x.NextTerm));
        }

        /// <summary>
        /// Reservations of everyone except the given user, restricted to a set of trainings.
        /// Used by the collaborative half of the recommender so it never loads the whole table.
        /// </summary>
        public async Task<List<(int UserId, int TrainingId)>> GetPeerReservationsAsync(
            int excludeUserId,
            IEnumerable<int> trainingIds,
            CancellationToken cancellationToken = default)
        {
            var ids = trainingIds.Distinct().ToList();
            if (ids.Count == 0) return new List<(int, int)>();

            var peerUserIds = await _dbSet
                .Where(r => !r.IsDeleted && r.UserId != excludeUserId && ids.Contains(r.TrainingId))
                .Select(r => r.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (peerUserIds.Count == 0) return new List<(int, int)>();

            var rows = await _dbSet
                .Where(r => !r.IsDeleted && peerUserIds.Contains(r.UserId))
                .Select(r => new { r.UserId, r.TrainingId })
                .ToListAsync(cancellationToken);

            return rows.Select(r => (r.UserId, r.TrainingId)).ToList();
        }

        /// <summary>
        /// Reservations that finished in the past and were paid for, so a scheduled or
        /// admin-triggered pass can move them to Completed through the state machine.
        /// </summary>
        public async Task<List<Reservation>> GetCompletableAsync(DateTime now, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(r => !r.IsDeleted && r.Status == ReservationStatus.Paid)
                .Include(r => r.Training)
                .Include(r => r.User)
                .Where(r => now > r.ReservationDate.AddMinutes(r.Training.DurationMinutes))
                .ToListAsync(cancellationToken);
        }
    }
}
