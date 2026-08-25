namespace FITSync.Contracts.Payments;

/// <summary>
/// Aggregates for the admin payments screen, computed in the database.
///
/// Review item 22: the desktop app used to load every payment row and sum it in
/// memory, which also meant the totals were wrong as soon as the list was paged.
/// </summary>
public class PaymentSummaryResponse
{
    /// <summary>Sum of captured payments only. Pending and failed do not count as revenue.</summary>
    public decimal TotalRevenue { get; set; }

    public int CapturedCount { get; set; }
    public int PayPalCount { get; set; }
    public int CashCount { get; set; }
}
