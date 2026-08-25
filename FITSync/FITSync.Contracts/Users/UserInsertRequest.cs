using System.ComponentModel.DataAnnotations;

namespace FITSync.Contracts.Users;

public class UserInsertRequest
{
    [Required(ErrorMessage = "Username is required.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters.")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
    public string Password { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Name { get; set; }

    [StringLength(50)]
    public string? Surname { get; set; }

    [Phone(ErrorMessage = "Invalid phone number.")]
    public string? PhoneNumber { get; set; }

    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Role to assign on creation. Defaults to Client; an administrator can create
    /// another administrator by sending "Administrator".
    /// </summary>
    [Required(ErrorMessage = "Role is required.")]
    [RegularExpression("^(Administrator|Client)$", ErrorMessage = "Role must be Administrator or Client.")]
    public string Role { get; set; } = "Client";
}
