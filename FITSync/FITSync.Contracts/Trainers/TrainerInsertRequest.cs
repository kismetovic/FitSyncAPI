using System.ComponentModel.DataAnnotations;

namespace FITSync.Contracts.Trainers;

public class TrainerInsertRequest
{
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters.")]
    public string LastName { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Biography { get; set; }

    [StringLength(80)]
    public string? Specialty { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email address.")]
    public string? Email { get; set; }

    [Phone(ErrorMessage = "Invalid phone number.")]
    public string? PhoneNumber { get; set; }

    [Range(0, 1000, ErrorMessage = "Surcharge cannot be negative.")]
    public decimal OutsideAvailabilitySurcharge { get; set; }

    public int? UserId { get; set; }
}
