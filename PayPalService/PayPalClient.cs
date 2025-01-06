namespace PayPalService;

public class PayPalClient
{
    private string Mode { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }

    public string BaseUrl => Mode == "sandbox" 
        ? PayPalEndpoints.SandboxBaseUrl 
        : PayPalEndpoints.LiveBaseUrl;

    public PayPalClient(string mode, string clientId, string clientSecret)
    {
        Mode = mode;
        ClientId = clientId;
        ClientSecret = clientSecret;
    }
}