using System.ComponentModel.DataAnnotations;

namespace FITSync.Contracts.Support;

public class SupportContactUpdateRequest
{
    [Required(ErrorMessage = "A support email is required.")]
    [EmailAddress(ErrorMessage = "That is not a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "A support phone number is required.")]
    [StringLength(40, MinimumLength = 6, ErrorMessage = "A phone number must be between 6 and 40 characters.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Working hours are required.")]
    [StringLength(120, ErrorMessage = "Working hours must be at most 120 characters.")]
    public string WorkingHours { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "An address must be at most 200 characters.")]
    public string? Address { get; set; }
}
