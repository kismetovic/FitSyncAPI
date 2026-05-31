namespace FITSync.Contracts.Users;

public class UserUpdateRequest
{
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
    public string? Surname { get; set; }
    public string? PhoneNumber { get; set; }
    public bool Enabled { get; set; }
}
