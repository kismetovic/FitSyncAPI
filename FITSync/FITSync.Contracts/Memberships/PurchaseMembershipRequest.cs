using System.ComponentModel.DataAnnotations;

namespace FITSync.Contracts.Memberships;

/// <summary>
/// The client picks a package; the server reads the price, duration and session count
/// from the package row. Nothing about the purchase is client-supplied beyond the choice.
/// </summary>
public class PurchaseMembershipRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "A valid membership package must be selected.")]
    public int MembershipPackageId { get; set; }

    /// <summary>Optional future start date; defaults to today when omitted.</summary>
    public DateTime? StartDate { get; set; }
}
