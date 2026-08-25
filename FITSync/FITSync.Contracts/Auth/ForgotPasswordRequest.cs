using System.ComponentModel.DataAnnotations;

namespace FITSync.Contracts.Auth;

public class ForgotPasswordRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    public string Email { get; set; } = string.Empty;

    [Url(ErrorMessage = "Reset base URL must be a valid URL.")]
    public string? ResetBaseUrl { get; set; }
}
