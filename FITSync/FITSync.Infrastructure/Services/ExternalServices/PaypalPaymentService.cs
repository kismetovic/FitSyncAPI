using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FITSync.Infrastructure.Configuration;
using FITSync.Infrastructure.Services.Interfaces;
using FITSync.Infrastructure.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FITSync.Infrastructure.Services.ExternalServices;

public class PaypalPaymentService : IPayPalPaymentService
{
    private readonly PayPalSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<PaypalPaymentService> _logger;

    public PaypalPaymentService(
        IOptions<PayPalSettings> options,
        HttpClient httpClient,
        ILogger<PaypalPaymentService> logger)
    {
        _settings = options.Value;
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Turns a PayPal error body into something a user can act on.
    ///
    /// PayPal answers with a JSON document containing a <c>details[].issue</c> code. The
    /// whole document used to be pushed into the exception message and travelled all the
    /// way to the phone screen, complete with debug ids and documentation links. Worse,
    /// it surfaced as "external service unavailable" even for ORDER_NOT_APPROVED, which
    /// simply means the payer has not approved the order yet - PayPal is working fine.
    ///
    /// The raw body is logged for diagnosis; the caller gets a stable code and a sentence.
    /// </summary>
    private Exception TranslatePayPalError(string operation, HttpResponseMessage response, string responseJson)
    {
        var status = (int)response.StatusCode;
        _logger.LogWarning("PayPal {Operation} failed ({Status}): {Body}", operation, status, responseJson);

        string? issue = null;
        try
        {
            using var doc = JsonDocument.Parse(responseJson);
            if (doc.RootElement.TryGetProperty("details", out var details) &&
                details.ValueKind == JsonValueKind.Array && details.GetArrayLength() > 0 &&
                details[0].TryGetProperty("issue", out var issueEl))
            {
                issue = issueEl.GetString();
            }
            else if (doc.RootElement.TryGetProperty("name", out var nameEl))
            {
                issue = nameEl.GetString();
            }
        }
        catch (JsonException)
        {
            // A non-JSON body means the problem is with the connection, not the order.
        }

        return issue switch
        {
            "ORDER_NOT_APPROVED" => new BusinessRuleException(
                "ORDER_NOT_APPROVED",
                "The payment has not been approved on PayPal yet. Finish the approval there, " +
                "then return to the app."),

            "ORDER_ALREADY_CAPTURED" => new BusinessRuleException(
                "ORDER_ALREADY_CAPTURED",
                "This PayPal order has already been captured."),

            "INSTRUMENT_DECLINED" => new BusinessRuleException(
                "INSTRUMENT_DECLINED",
                "PayPal declined the selected payment method. Try another card or account."),

            "PAYER_ACTION_REQUIRED" => new BusinessRuleException(
                "PAYER_ACTION_REQUIRED",
                "PayPal requires an extra confirmation. Open PayPal and finish the payment."),

            // PayPal blocked the transaction at its end. In sandbox this is almost
            // always the test accounts themselves - a buyer or merchant account that
            // cannot transact in the order currency - rather than anything the caller
            // did wrong. Either way it is a refusal, not an outage, so it must not be
            // reported as "external service unavailable".
            "COMPLIANCE_VIOLATION" => new BusinessRuleException(
                "COMPLIANCE_VIOLATION",
                "PayPal refused this transaction. The payment was not taken. " +
                "Please use a different payment method or contact the gym."),

            "PAYEE_ACCOUNT_RESTRICTED" => new BusinessRuleException(
                "PAYEE_ACCOUNT_RESTRICTED",
                "The gym's PayPal account cannot accept this payment right now."),

            "CURRENCY_NOT_SUPPORTED" => new BusinessRuleException(
                "CURRENCY_NOT_SUPPORTED",
                "PayPal does not support this currency for one of the accounts involved."),

            "TRANSACTION_REFUSED" => new BusinessRuleException(
                "TRANSACTION_REFUSED",
                "PayPal refused this transaction. The payment was not taken."),

            // Anything else genuinely is an upstream fault.
            _ => new HttpRequestException(
                $"PayPal {operation} failed with status {status}" +
                (issue is null ? "." : $" ({issue}).")),
        };
    }

    public async Task<PayPalOrderResult> CreateOrderAsync(decimal amount, string currency, string reference, CancellationToken cancellationToken = default)
    {
        var token = await GetAccessTokenAsync(cancellationToken);

        var orderRequest = new
        {
            intent = "CAPTURE",
            purchase_units = new[]
            {
                new
                {
                    // Read back on capture and checked against what is being paid for.
                    reference_id = reference,
                    custom_id = reference,
                    description = $"FITSync {reference}",
                    amount = new
                    {
                        currency_code = currency.ToUpperInvariant(),
                        value = amount.ToString("F2", CultureInfo.InvariantCulture)
                    }
                }
            },
            application_context = new
            {
                // Falling back to example.com left the client staring at a placeholder
                // page after paying, with no way back and no idea anything was pending.
                return_url = string.IsNullOrWhiteSpace(_settings.ReturnUrl) ? "fitsync://paypal/return" : _settings.ReturnUrl,
                cancel_url = string.IsNullOrWhiteSpace(_settings.CancelUrl) ? "fitsync://paypal/cancel" : _settings.CancelUrl,
                user_action = "PAY_NOW",
                brand_name = "FITSync",

                // A training session is not posted anywhere, so the checkout should
                // neither ask for nor display a delivery address. Without this PayPal
                // showed "Ship to ..." above a gym booking, which is simply wrong.
                shipping_preference = "NO_SHIPPING"
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl}/v2/checkout/orders")
        {
            Content = new StringContent(JsonSerializer.Serialize(orderRequest), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw TranslatePayPalError("order creation", response, responseJson);

        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;

        var approvalUrl = "";
        if (root.TryGetProperty("links", out var links))
        {
            foreach (var link in links.EnumerateArray())
            {
                if (link.TryGetProperty("rel", out var rel) && rel.GetString() == "approve")
                {
                    approvalUrl = link.GetProperty("href").GetString() ?? "";
                    break;
                }
            }
        }

        return new PayPalOrderResult
        {
            OrderId = root.GetProperty("id").GetString() ?? "",
            ApprovalUrl = approvalUrl
        };
    }

    /// <summary>
    /// Captures and returns everything needed for verification: status, capture id, and the
    /// amount, currency and reference PayPal actually recorded. The caller compares those
    /// against the reservation before writing a payment.
    /// </summary>
    public async Task<PayPalCaptureResult> CaptureOrderAsync(string orderId, CancellationToken cancellationToken = default)
    {
        var token = await GetAccessTokenAsync(cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl}/v2/checkout/orders/{orderId}/capture")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw TranslatePayPalError("capture", response, responseJson);

        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;

        var result = new PayPalCaptureResult
        {
            Status = root.TryGetProperty("status", out var statusEl) ? statusEl.GetString() ?? "" : ""
        };

        if (root.TryGetProperty("purchase_units", out var units) && units.GetArrayLength() > 0)
        {
            var unit = units[0];

            if (unit.TryGetProperty("reference_id", out var refEl))
                result.ReferenceId = refEl.GetString() ?? "";

            if (unit.TryGetProperty("payments", out var payments) &&
                payments.TryGetProperty("captures", out var captures) &&
                captures.GetArrayLength() > 0)
            {
                var capture = captures[0];

                if (capture.TryGetProperty("id", out var idEl))
                    result.TransactionId = idEl.GetString() ?? "";

                // The capture's own status is more precise than the order-level one.
                if (capture.TryGetProperty("status", out var capStatus))
                    result.Status = capStatus.GetString() ?? result.Status;

                if (capture.TryGetProperty("amount", out var amountEl))
                {
                    if (amountEl.TryGetProperty("value", out var valueEl) &&
                        decimal.TryParse(valueEl.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
                        result.Amount = value;

                    if (amountEl.TryGetProperty("currency_code", out var currencyEl))
                        result.Currency = currencyEl.GetString() ?? "";
                }
            }
        }

        return result;
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.ClientId) || string.IsNullOrWhiteSpace(_settings.ClientSecret))
            throw new InvalidOperationException("PayPal credentials are not configured (PayPal__ClientId / PayPal__ClientSecret).");

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_settings.ClientId}:{_settings.ClientSecret}"));

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl}/v1/oauth2/token")
        {
            Content = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("grant_type", "client_credentials") })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw TranslatePayPalError("authentication", response, json);

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("access_token").GetString() ?? "";
    }
}
