namespace FITSync.Contracts.Memberships;

public class MembershipPackageResponse
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationDays { get; set; }
    public int SessionCount { get; set; }
    public decimal Price { get; set; }
    public int? TrainingTypeId { get; set; }
    public string? TrainingTypeName { get; set; }
    public bool IsActive { get; set; }

    /// <summary>Effective price of a single session inside the package.</summary>
    public decimal PricePerSession => SessionCount <= 0 ? 0 : Math.Round(Price / SessionCount, 2);
}
