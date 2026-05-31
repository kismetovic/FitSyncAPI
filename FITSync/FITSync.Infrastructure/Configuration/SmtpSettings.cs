namespace FITSync.Infrastructure.Configuration;

public class SmtpSettings
{
    public const string SectionName = "Smtp";
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public string FromEmail { get; set; } = "";
    public string FromName { get; set; } = "FitSync";
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
    public bool EnableSsl { get; set; } = true;
}
