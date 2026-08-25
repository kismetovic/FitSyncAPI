using FITSync.Contracts.Reports;
using FITSync.Domain.Enums;
using FITSync.Infrastructure.Repositories.Interfaces;
using FITSync.Infrastructure.Services.Interfaces;

namespace FITSync.Infrastructure.Services
{
    /// <summary>
    /// Produces the numbers behind the two desktop PDF reports. The desktop app renders
    /// what this returns and computes nothing of its own.
    /// </summary>
    public class ReportService : IReportService
    {
        private readonly IReservationRepository _reservationRepository;
        private readonly IPaymentRepository _paymentRepository;

        public ReportService(IReservationRepository reservationRepository, IPaymentRepository paymentRepository)
        {
            _reservationRepository = reservationRepository;
            _paymentRepository = paymentRepository;
        }

        public async Task<ReservationReportResponse> GetReservationReportAsync(
            ReservationReportRequest request, CancellationToken cancellationToken = default)
        {
            var from = request.From.Date;
            var to = request.To.Date.AddDays(1).AddTicks(-1);

            var reservations = await _reservationRepository.GetForReportAsync(from, to, request.TrainingId, cancellationToken);

            var rows = reservations.Select(r => new ReservationReportRow
            {
                ReservationId = r.Id,
                ReservationDate = r.ReservationDate,
                TrainingName = r.Training?.Name ?? "-",
                TrainerName = r.Training?.Trainer?.FullName,
                ClientName = BuildClientName(r.User?.Name, r.User?.Surname, r.User?.UserName),
                Status = r.Status,
                ReservationType = r.ReservationType,
                TotalPrice = r.TotalPrice,
                IsPaid = r.Payments?.Any(p => !p.IsDeleted && p.Status == PaymentStatus.Captured) ?? false
            }).ToList();

            return new ReservationReportResponse
            {
                From = from,
                To = request.To.Date,
                GeneratedAt = DateTime.UtcNow,
                TotalReservations = rows.Count,
                CancelledReservations = rows.Count(r => r.Status == ReservationStatus.Cancelled),
                CompletedReservations = rows.Count(r => r.Status == ReservationStatus.Completed),
                PaidReservations = rows.Count(r => r.IsPaid),
                TotalValue = rows.Where(r => r.Status != ReservationStatus.Cancelled).Sum(r => r.TotalPrice),
                Rows = rows,
                StatusBreakdown = rows
                    .GroupBy(r => r.Status)
                    .Select(g => new ReservationReportStatusCount { Status = g.Key, Count = g.Count() })
                    .OrderBy(s => s.Status)
                    .ToList()
            };
        }

        public async Task<RevenueReportResponse> GetRevenueReportAsync(
            RevenueReportRequest request, CancellationToken cancellationToken = default)
        {
            var from = request.From.Date;
            var to = request.To.Date.AddDays(1).AddTicks(-1);

            // Only captured payments count as revenue.
            var payments = await _paymentRepository.GetCapturedInPeriodAsync(from, to, cancellationToken);

            var rows = payments
                .Where(p => p.Reservation?.Training != null)
                .GroupBy(p => p.Reservation!.Training!)
                .Select(g => new RevenueReportRow
                {
                    TrainingId = g.Key.Id,
                    TrainingName = g.Key.Name,
                    TrainerName = g.Key.Trainer?.FullName,
                    TrainingTypeName = g.Key.TrainingType?.Name,
                    PaymentsCount = g.Count(),
                    Revenue = g.Sum(p => p.Amount),
                    AveragePayment = Math.Round(g.Average(p => p.Amount), 2)
                })
                .OrderByDescending(r => r.Revenue)
                .ToList();

            // Package sales are revenue too, but they hang off a membership rather than a
            // training, so grouping by training alone silently dropped them from the
            // breakdown while TotalRevenue below still counted them - the report would not
            // have added up. They get their own rows, one per package.
            var packageRows = payments
                .Where(p => p.Reservation == null && p.UserMembership?.MembershipPackage != null)
                .GroupBy(p => p.UserMembership!.MembershipPackage!)
                .Select(g => new RevenueReportRow
                {
                    TrainingId = 0,
                    TrainingName = g.Key.Name,
                    TrainerName = null,
                    TrainingTypeName = "Mjesečni paket",
                    PaymentsCount = g.Count(),
                    Revenue = g.Sum(p => p.Amount),
                    AveragePayment = Math.Round(g.Average(p => p.Amount), 2)
                })
                .OrderByDescending(r => r.Revenue)
                .ToList();

            rows.AddRange(packageRows);

            return new RevenueReportResponse
            {
                From = from,
                To = request.To.Date,
                GeneratedAt = DateTime.UtcNow,
                TotalRevenue = payments.Sum(p => p.Amount),
                TotalPayments = payments.Count,
                Currency = payments.FirstOrDefault()?.Currency ?? "BAM",
                Rows = rows,
                ProviderBreakdown = payments
                    .GroupBy(p => p.PaymentProvider)
                    .Select(g => new RevenueByProviderRow
                    {
                        Provider = g.Key,
                        PaymentsCount = g.Count(),
                        Revenue = g.Sum(p => p.Amount)
                    })
                    .OrderByDescending(p => p.Revenue)
                    .ToList()
            };
        }

        private static string BuildClientName(string? name, string? surname, string? userName)
        {
            var full = $"{name} {surname}".Trim();
            return string.IsNullOrWhiteSpace(full) ? (userName ?? "-") : full;
        }
    }
}
