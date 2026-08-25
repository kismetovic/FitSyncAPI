using FITSync.Domain.Enums;

namespace FITSync.Contracts.Reports;

/// <summary>
/// Backing data for the desktop "Reservations by period" PDF. Everything the report
/// prints - including the totals - is computed here on the server.
/// </summary>
public class ReservationReportResponse
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public DateTime GeneratedAt { get; set; }

    public int TotalReservations { get; set; }
    public int CancelledReservations { get; set; }
    public int CompletedReservations { get; set; }
    public int PaidReservations { get; set; }
    public decimal TotalValue { get; set; }

    public List<ReservationReportRow> Rows { get; set; } = new();
    public List<ReservationReportStatusCount> StatusBreakdown { get; set; } = new();
}

public class ReservationReportRow
{
    public int ReservationId { get; set; }
    public DateTime ReservationDate { get; set; }
    public string TrainingName { get; set; } = string.Empty;
    public string? TrainerName { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public ReservationStatus Status { get; set; }
    public ReservationType ReservationType { get; set; }
    public decimal TotalPrice { get; set; }
    public bool IsPaid { get; set; }
}

public class ReservationReportStatusCount
{
    public ReservationStatus Status { get; set; }
    public int Count { get; set; }
}
