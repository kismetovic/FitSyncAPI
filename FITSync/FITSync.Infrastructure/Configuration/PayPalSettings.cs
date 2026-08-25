namespace FITSync.Infrastructure.Configuration;

public class PayPalSettings
{
    public const string SectionName = "PayPal";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string BaseUrl { get; set; } = "https://api-m.sandbox.paypal.com";

    /// <summary>Where PayPal sends the buyer after approval. Configured per environment.</summary>
    /// <summary>
    /// Where PayPal sends the browser after the client approves or cancels.
    ///
    /// The default is the app's own deep link, so finishing at PayPal brings the
    /// client straight back to FitSync and the automatic verification starts. It
    /// used to point at example.com, which meant the client landed on a blank
    /// placeholder page with no idea the payment still had to be confirmed - and
    /// so the capture was never requested.
    ///
    /// Override through PayPal__ReturnUrl / PayPal__CancelUrl when the app is
    /// reached some other way.
    /// </summary>
    public string ReturnUrl { get; set; } = "fitsync://paypal/return";
    public string CancelUrl { get; set; } = "fitsync://paypal/cancel";
}
