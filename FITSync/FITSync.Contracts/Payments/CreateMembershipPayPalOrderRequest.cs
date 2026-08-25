using System.ComponentModel.DataAnnotations;

namespace FITSync.Contracts.Payments;

/// <summary>
/// Naming the package to pay for. Like the reservation equivalent it carries no amount:
/// the server reads the price from the package the client bought.
/// </summary>
public class CreateMembershipPayPalOrderRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "A membership id is required.")]
    public int MembershipId { get; set; }
}
