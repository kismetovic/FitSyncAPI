namespace FITSync.Infrastructure.Configuration;

public class PayPalSettings
{
    public const string SectionName = "PayPal";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string BaseUrl { get; set; } = "https://api-m.sandbox.paypal.com";
}
