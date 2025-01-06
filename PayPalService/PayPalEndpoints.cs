namespace PayPalService;

public static class PayPalEndpoints
{
    public const string SandboxBaseUrl = "https://api-m.sandbox.paypal.com";
    public const string LiveBaseUrl = "https://api-m.paypal.com";
    public const string TokenEndpoint = "/v1/oauth2/token";
    public const string PaymentEndpoint = "/v2/checkout/orders";
    public const string PaymentWithCardEndpoint = "/v2/vault/payment-tokens";
}