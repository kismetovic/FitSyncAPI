using FITSync.Contracts.Common;
using FITSync.Domain.Enums;

namespace FITSync.Contracts.Payments;

public class PaymentSearchRequest : PagedRequest
{
    public int? UserId { get; set; }
    public int? ReservationId { get; set; }
    public PaymentStatus? Status { get; set; }
    public PaymentProvider? Provider { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Free-text match over the client's name, the training name or the provider
    /// transaction id. Applied in SQL so the admin search still spans every page.
    /// </summary>
    public string? Query { get; set; }
}
