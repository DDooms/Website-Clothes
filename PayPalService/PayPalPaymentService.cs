using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace PayPalService;

public class PayPalPaymentService : IPayPalService
{
    private readonly PayPalClient _payPalClient;

    public PayPalPaymentService(PayPalClient payPalClient)
    {
        _payPalClient = payPalClient;
    }

    private async Task<string> GetAccessTokenAsync()
    {
        using var httpClient = new HttpClient();
        var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_payPalClient.ClientId}:{_payPalClient.ClientSecret}"));
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);

        var tokenResponse = await httpClient.PostAsync(
            $"{_payPalClient.BaseUrl}{PayPalEndpoints.TokenEndpoint}",
            new FormUrlEncodedContent([
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            ]));

        if (!tokenResponse.IsSuccessStatusCode)
        {
            var response = await tokenResponse.Content.ReadAsStringAsync();
            throw new Exception($"Failed to retrieve PayPal access token. Response: {response}");
        }

        var tokenResult = await tokenResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        return tokenResult?["access_token"].ToString() ?? throw new Exception("Access token is null or empty.");
    }

    public async Task<string> CreateOrderWithPaymentMethod(decimal amount, string currency, string paymentMethodToken, string returnUrl, string cancelUrl)
    {
        var accessToken = await GetAccessTokenAsync();

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var orderPayload = new
        {
            intent = "CAPTURE",
            purchase_units = new[]
            {
                new
                {
                    amount = new
                    {
                        currency_code = currency,
                        value = amount.ToString("F2")
                    }
                }
            },
            payment_source = string.IsNullOrEmpty(paymentMethodToken)
                ? null // Use PayPal payment flow when token is not provided
                : new
                {
                    token = new
                    {
                        id = paymentMethodToken,
                        type = "PAYMENT_METHOD_TOKEN"
                    }
                },
            application_context = new
            {
                return_url = returnUrl,
                cancel_url = cancelUrl
            }
        };

        var orderResponse = await httpClient.PostAsJsonAsync(
            $"{_payPalClient.BaseUrl}{PayPalEndpoints.PaymentEndpoint}",
            orderPayload);

        if (!orderResponse.IsSuccessStatusCode)
        {
            var response = await orderResponse.Content.ReadAsStringAsync();
            throw new Exception($"Failed to create PayPal order. Response: {response}");
        }

        var orderResult = await orderResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
    
        // Extract approval URL for PayPal payments
        if (string.IsNullOrEmpty(paymentMethodToken))
        {
            var approvalLink = ((JsonElement)orderResult?["links"]!).EnumerateArray()
                .First(link => link.GetProperty("rel").GetString() == "approve")
                .GetProperty("href").GetString();
        
            return approvalLink ?? throw new Exception("Approval link not found.");
        }

        // For card payments, return order ID
        return orderResult?["id"]?.ToString() ?? throw new Exception("Order ID not found.");
    }



    public async Task<string> CaptureOrder(string token, string payerId)
    {
        var accessToken = await GetAccessTokenAsync();

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // PayPal endpoint for completing payment
        var captureEndpoint = $"{_payPalClient.BaseUrl}{PayPalEndpoints.PaymentEndpoint}/{token}/capture";

        // Prepare request body
        var content = new StringContent($"{{\"payer_id\": \"{payerId}\"}}", Encoding.UTF8, "application/json");

        var captureResponse = await httpClient.PostAsync(captureEndpoint, content);

        if (!captureResponse.IsSuccessStatusCode)
        {
            var response = await captureResponse.Content.ReadAsStringAsync();
            throw new Exception($"Failed to capture PayPal payment. Response: {response}");
        }

        return await captureResponse.Content.ReadAsStringAsync();
    }
    
    public async Task<string> CreatePaymentMethod(string cardNumber, string expiryDate, string cvv)
    {
        var accessToken = await GetAccessTokenAsync();

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var payload = new
        {
            type = "CARD",
            source = new
            {
                card = new
                {
                    number = cardNumber,
                    expiry = expiryDate, // Ensure this is in "YYYY-MM" format
                    security_code = cvv
                }
            }
        };

        var response = await httpClient.PostAsJsonAsync(
            $"{_payPalClient.BaseUrl}{PayPalEndpoints.PaymentWithCardEndpoint}",
            payload);

        if (!response.IsSuccessStatusCode)
        {
            var errorResponse = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to tokenize card. Response: {errorResponse}");
        }

        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        return result?["id"].ToString() ?? throw new Exception("Payment method token not found.");
    }
}