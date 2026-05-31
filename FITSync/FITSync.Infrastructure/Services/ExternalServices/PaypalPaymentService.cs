using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FITSync.Infrastructure.Configuration;
using FITSync.Infrastructure.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace FITSync.Infrastructure.Services.ExternalServices;

public class PaypalPaymentService : IPayPalPaymentService
{
    private readonly PayPalSettings _settings;
    private readonly HttpClient _httpClient;

    public PaypalPaymentService(IOptions<PayPalSettings> options, HttpClient httpClient)
    {
        _settings = options.Value;
        _httpClient = httpClient;
    }

    public async Task<PayPalOrderResult> CreateOrderAsync(decimal amount, string currency, int reservationId, CancellationToken cancellationToken = default)
    {
        var token = await GetAccessTokenAsync(cancellationToken);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var orderRequest = new
        {
            intent = "CAPTURE",
            purchase_units = new[]
            {
                new
                {
                    reference_id = $"reservation-{reservationId}",
                    amount = new
                    {
                        currency_code = currency.ToUpperInvariant(),
                        value = amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                    }
                }
            },
            application_context = new
            {
                return_url = "https://example.com/paypal/return",
                cancel_url = "https://example.com/paypal/cancel",
                user_action = "PAY_NOW",
                brand_name = "FITSync"
            }
        };

        var json = JsonSerializer.Serialize(orderRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{_settings.BaseUrl}/v2/checkout/orders", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;
        var orderId = root.GetProperty("id").GetString() ?? "";
        string approvalUrl = "";
        var links = root.GetProperty("links");
        foreach (var link in links.EnumerateArray())
        {
            if (link.GetProperty("rel").GetString() == "approve")
            {
                approvalUrl = link.GetProperty("href").GetString() ?? "";
                break;
            }
        }

        return new PayPalOrderResult { OrderId = orderId, ApprovalUrl = approvalUrl };
    }

    public async Task<PayPalCaptureResult> CaptureOrderAsync(string orderId, CancellationToken cancellationToken = default)
    {
        var token = await GetAccessTokenAsync(cancellationToken);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.PostAsync($"{_settings.BaseUrl}/v2/checkout/orders/{orderId}/capture", null, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;
        var status = root.GetProperty("status").GetString() ?? "";
        string transactionId = "";
        if (root.TryGetProperty("purchase_units", out var units) && units.GetArrayLength() > 0)
        {
            var first = units[0];
            if (first.TryGetProperty("payments", out var payments) && payments.TryGetProperty("captures", out var captures) && captures.GetArrayLength() > 0)
            {
                transactionId = captures[0].GetProperty("id").GetString() ?? "";
            }
        }

        return new PayPalCaptureResult { TransactionId = transactionId, Status = status };
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_settings.ClientId}:{_settings.ClientSecret}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        var form = new List<KeyValuePair<string, string>>
        {
            new("grant_type", "client_credentials")
        };
        var content = new FormUrlEncodedContent(form);
        var response = await _httpClient.PostAsync($"{_settings.BaseUrl}/v1/oauth2/token", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("access_token").GetString() ?? "";
    }
}
