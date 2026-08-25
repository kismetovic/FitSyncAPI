using System.ComponentModel.DataAnnotations;

namespace FITSync.Contracts.Payments;

public class CaptureMembershipPayPalOrderRequest
{
    [Required(ErrorMessage = "A PayPal order id is required.")]
    public string OrderId { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "A membership id is required.")]
    public int MembershipId { get; set; }
}
