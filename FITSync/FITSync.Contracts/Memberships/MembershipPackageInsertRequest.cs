using System.ComponentModel.DataAnnotations;

namespace FITSync.Contracts.Memberships;

public class MembershipPackageInsertRequest
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(80, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 80 characters.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Range(1, 366, ErrorMessage = "Duration must be between 1 and 366 days.")]
    public int DurationDays { get; set; } = 30;

    [Range(1, 200, ErrorMessage = "Session count must be at least 1.")]
    public int SessionCount { get; set; }

    [Range(0.01, 100000, ErrorMessage = "Price must be greater than zero.")]
    public decimal Price { get; set; }

    public int? TrainingTypeId { get; set; }

    public bool IsActive { get; set; } = true;
}
