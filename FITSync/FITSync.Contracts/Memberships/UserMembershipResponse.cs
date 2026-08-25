using FITSync.Domain.Enums;

namespace FITSync.Contracts.Memberships;

public class UserMembershipResponse
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public int UserId { get; set; }
    public int MembershipPackageId { get; set; }
    public string? MembershipPackageName { get; set; }
    public int? TrainingTypeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int SessionsTotal { get; set; }
    public int SessionsUsed { get; set; }
    public int SessionsRemaining { get; set; }
    public MembershipStatus Status { get; set; }
    public decimal PricePaid { get; set; }
    public bool IsUsable { get; set; }
}
