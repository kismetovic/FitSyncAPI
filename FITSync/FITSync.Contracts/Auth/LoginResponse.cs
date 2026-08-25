namespace FITSync.Contracts.Auth;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Roles of the signed-in user, so the desktop app can refuse to open the admin shell
    /// without a second round-trip. The backend still enforces every rule itself.
    /// </summary>
    public List<string> Roles { get; set; } = new();
}
