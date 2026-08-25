using System.ComponentModel.DataAnnotations;

namespace FITSync.Contracts.Payments;

public class ConfirmMembershipCashPaymentRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "A membership id is required.")]
    public int MembershipId { get; set; }

    [StringLength(300, ErrorMessage = "A note must be at most 300 characters.")]
    public string? Note { get; set; }
}
