using FITSync.Domain.Enums;

namespace FITSync.Contracts.Reports;

/// <summary>
/// Backing data for the desktop "Revenue by training" PDF. Only captured payments count
/// towards revenue.
/// </summary>
public class RevenueReportResponse
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public DateTime GeneratedAt { get; set; }

    public decimal TotalRevenue { get; set; }
    public int TotalPayments { get; set; }
    public string Currency { get; set; } = "BAM";

    public List<RevenueReportRow> Rows { get; set; } = new();
    public List<RevenueByProviderRow> ProviderBreakdown { get; set; } = new();
}

public class RevenueReportRow
{
    public int TrainingId { get; set; }
    public string TrainingName { get; set; } = string.Empty;
    public string? TrainerName { get; set; }
    public string? TrainingTypeName { get; set; }
    public int PaymentsCount { get; set; }
    public decimal Revenue { get; set; }
    public decimal AveragePayment { get; set; }
}

public class RevenueByProviderRow
{
    public PaymentProvider Provider { get; set; }
    public int PaymentsCount { get; set; }
    public decimal Revenue { get; set; }
}
