using System.ComponentModel.DataAnnotations;
using FITSync.Contracts.Common;

namespace FITSync.Contracts.Users;

public class UserSearchRequest : PagedRequest
{
    [StringLength(100)]
    public string? Name { get; set; }

    [RegularExpression("^(Administrator|Client)$", ErrorMessage = "Role must be Administrator or Client.")]
    public string? Role { get; set; }

    public bool? Enabled { get; set; }
}
