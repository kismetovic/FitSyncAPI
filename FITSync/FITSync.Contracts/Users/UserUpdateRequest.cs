using System.ComponentModel.DataAnnotations;

namespace FITSync.Contracts.Users;

public class UserUpdateRequest
{
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters.")]
    public string? UserName { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email address.")]
    public string? Email { get; set; }

    [StringLength(50)]
    public string? Name { get; set; }

    [StringLength(50)]
    public string? Surname { get; set; }

    [Phone(ErrorMessage = "Invalid phone number.")]
    public string? PhoneNumber { get; set; }

    public bool Enabled { get; set; }

    /// <summary>
    /// New role for the user. Null leaves the current role untouched; any other value
    /// actually rewrites the Identity role assignment, so the desktop dropdown now has
    /// a real effect on backend state.
    /// </summary>
    [RegularExpression("^(Administrator|Client)$", ErrorMessage = "Role must be Administrator or Client.")]
    public string? Role { get; set; }
}
