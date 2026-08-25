namespace FITSync.Contracts.Payments;

public class CreatePayPalOrderResponse
{
    public string OrderId { get; set; } = string.Empty;

    /// <summary>Official PayPal approval URL the user must be sent to.</summary>
    public string ApprovalUrl { get; set; } = string.Empty;

    /// <summary>Server-calculated amount, echoed back for display only.</summary>
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "BAM";
    /// <summary>What PayPal actually charges. The gym prices in BAM, which PayPal
    /// cannot take, so the order is placed in the pegged euro equivalent.</summary>
    public decimal ChargedAmount { get; set; }
    public string ChargedCurrency { get; set; } = "EUR";

    public int? ReservationId { get; set; }

    public int? UserMembershipId { get; set; }
}
