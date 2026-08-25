using FITSync.Domain.Entities;
using FITSync.Domain.Enums;
using FITSync.Infrastructure.Context;
using FITSync.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FITSync.Infrastructure.Repositories
{
    public class PaymentRepository : BaseRepository<Payment>, IPaymentRepository
    {
        public PaymentRepository(FitSyncDbContext context) : base(context)
        {
        }

        protected override IQueryable<Payment> BaseQuery()
        {
            return _dbSet
                .Where(p => !p.IsDeleted)
                .Include(p => p.Reservation).ThenInclude(r => r!.User)
                .Include(p => p.Reservation).ThenInclude(r => r!.Training)
                // A payment for a package has no reservation, so the package has to be
                // loaded too or the response comes back with nothing naming what was paid.
                .Include(p => p.UserMembership).ThenInclude(m => m!.MembershipPackage)
                .Include(p => p.UserMembership).ThenInclude(m => m!.User);
        }

        /// <summary>Most recent attempt for a reservation, whatever its status.</summary>
        public async Task<Payment?> GetByReservationIdAsync(int reservationId, CancellationToken cancellationToken = default)
        {
            return await BaseQuery()
                .Where(p => p.ReservationId == reservationId)
                .OrderByDescending(p => p.Status == PaymentStatus.Captured)
                .ThenByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        /// <summary>
        /// The one successful payment for a reservation, if any. This is the check that
        /// stops a reservation being paid twice.
        /// </summary>
        public async Task<Payment?> GetCapturedByReservationIdAsync(int reservationId, CancellationToken cancellationToken = default)
        {
            return await BaseQuery()
                .FirstOrDefaultAsync(p => p.ReservationId == reservationId && p.Status == PaymentStatus.Captured, cancellationToken);
        }

        /// <summary>Idempotency lookup: has this PayPal order already been recorded?</summary>
        public async Task<Payment?> GetByProviderOrderIdAsync(string providerOrderId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(providerOrderId)) return null;
            return await BaseQuery()
                .FirstOrDefaultAsync(p => p.ProviderOrderId == providerOrderId, cancellationToken);
        }

        public async Task<Payment?> GetByTransactionIdAsync(string transactionId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(transactionId)) return null;
            return await BaseQuery()
                .FirstOrDefaultAsync(p => p.TransactionId == transactionId, cancellationToken);
        }

        public async Task<List<Payment>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            // A user's payments are the ones for their bookings *and* the ones for the
            // packages they bought. Only the first half was matched here, which is why a
            // bought package never appeared under "my payments".
            return await BaseQuery()
                .Where(p => (p.Reservation != null && p.Reservation.UserId == userId)
                            || (p.UserMembership != null && p.UserMembership.UserId == userId))
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<(List<Payment> Items, int TotalCount)> SearchAsync(
            int? userId,
            int? reservationId,
            PaymentStatus? status,
            PaymentProvider? provider,
            DateTime? fromDate,
            DateTime? toDate,
            string? searchTerm,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            var query = BaseQuery();

            // Package payments belong to the buyer just as booking payments belong to
            // the person who booked, so both have to be matched here.
            if (userId.HasValue)
                query = query.Where(p =>
                    (p.Reservation != null && p.Reservation.UserId == userId.Value)
                    || (p.UserMembership != null && p.UserMembership.UserId == userId.Value));
            if (reservationId.HasValue)
                query = query.Where(p => p.ReservationId == reservationId.Value);
            if (status.HasValue)
                query = query.Where(p => p.Status == status.Value);
            if (provider.HasValue)
                query = query.Where(p => p.PaymentProvider == provider.Value);
            if (fromDate.HasValue)
                query = query.Where(p => p.CreatedAt >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(p => p.CreatedAt <= toDate.Value);

            // Matched in SQL so the admin search covers every page, not just the
            // rows the client currently holds.
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                query = query.Where(p =>
                    (p.TransactionId != null && p.TransactionId.Contains(term)) ||
                    (p.Reservation != null && (
                        (p.Reservation.User != null && (
                            (p.Reservation.User.Name != null && p.Reservation.User.Name.Contains(term)) ||
                            (p.Reservation.User.Surname != null && p.Reservation.User.Surname.Contains(term)) ||
                            (p.Reservation.User.UserName != null && p.Reservation.User.UserName.Contains(term)))) ||
                        (p.Reservation.Training != null && p.Reservation.Training.Name.Contains(term)))) ||
                    (p.UserMembership != null && (
                        (p.UserMembership.User != null && (
                            (p.UserMembership.User.Name != null && p.UserMembership.User.Name.Contains(term)) ||
                            (p.UserMembership.User.Surname != null && p.UserMembership.User.Surname.Contains(term)) ||
                            (p.UserMembership.User.UserName != null && p.UserMembership.User.UserName.Contains(term)))) ||
                        (p.UserMembership.MembershipPackage != null &&
                         p.UserMembership.MembershipPackage.Name.Contains(term)))));
            }

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);

            return (items, total);
        }

        public async Task<List<Payment>> GetCapturedInPeriodAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(p => !p.IsDeleted
                            && p.Status == PaymentStatus.Captured
                            && p.CapturedAt != null
                            && p.CapturedAt >= from
                            && p.CapturedAt <= to)
                .Include(p => p.Reservation).ThenInclude(r => r!.Training).ThenInclude(t => t.TrainingType)
                .Include(p => p.Reservation).ThenInclude(r => r!.Training).ThenInclude(t => t.Trainer)
                .Include(p => p.Reservation).ThenInclude(r => r!.User)
                // Package sales appear in the revenue report too, so the package has to
                // come along or its rows would have no name.
                .Include(p => p.UserMembership).ThenInclude(m => m!.MembershipPackage)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Revenue aggregated in the database. Only captured payments count, so a pending or
        /// failed attempt never inflates the dashboard total.
        /// </summary>
        public async Task<decimal> GetTotalCapturedRevenueAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Where(p => !p.IsDeleted && p.Status == PaymentStatus.Captured)
                .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;
        }

        /// <summary>
        /// One grouped round trip for the admin summary cards. Counting and summing
        /// here keeps the totals correct no matter which page the client is showing.
        /// </summary>
        public async Task<(decimal TotalRevenue, int CapturedCount, int PayPalCount, int CashCount)>
            GetSummaryAsync(CancellationToken cancellationToken = default)
        {
            var captured = _dbSet.Where(p => !p.IsDeleted && p.Status == PaymentStatus.Captured);

            var byProvider = await captured
                .GroupBy(p => p.PaymentProvider)
                .Select(g => new
                {
                    Provider = g.Key,
                    Count = g.Count(),
                    Total = g.Sum(p => (decimal?)p.Amount) ?? 0m
                })
                .ToListAsync(cancellationToken);

            return (
                byProvider.Sum(g => g.Total),
                byProvider.Sum(g => g.Count),
                byProvider.Where(g => g.Provider == PaymentProvider.PayPal).Sum(g => g.Count),
                byProvider.Where(g => g.Provider == PaymentProvider.Cash).Sum(g => g.Count));
        }

        public override async Task<Payment?> GetByIdAsync(int id)
        {
            return await BaseQuery().FirstOrDefaultAsync(p => p.Id == id);
        }

        public override async Task<List<Payment>> GetAsync()
        {
            return await BaseQuery().OrderByDescending(p => p.CreatedAt).ToListAsync();
        }
    }
}
