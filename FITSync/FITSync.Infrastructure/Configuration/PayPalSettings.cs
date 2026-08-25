namespace FITSync.Infrastructure.Configuration;

public class PayPalSettings
{
    public const string SectionName = "PayPal";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string BaseUrl { get; set; } = "https://api-m.sandbox.paypal.com";

    /// <summary>Where PayPal sends the buyer after approval. Configured per environment.</summary>
    public string ReturnUrl { get; set; } = "";
    public string CancelUrl { get; set; } = "";
}
