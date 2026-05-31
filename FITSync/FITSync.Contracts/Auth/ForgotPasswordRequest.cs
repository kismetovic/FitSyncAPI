namespace FITSync.Contracts.Auth;

public class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
    public string ResetBaseUrl { get; set; } = string.Empty;
}
