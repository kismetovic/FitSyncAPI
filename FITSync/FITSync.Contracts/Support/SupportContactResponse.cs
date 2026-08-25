namespace FITSync.Contracts.Support;

public class SupportContactResponse
{
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string WorkingHours { get; set; } = string.Empty;
    public string? Address { get; set; }
}
