namespace PayPalService;

public interface IPayPalService
{
    Task<string> CreateOrderWithPaymentMethod(decimal amount, string currency, string paymentMethodToken, string returnUrl, string cancelUrl);
    Task<string> CaptureOrder(string token, string payerId);
    Task<string> CreatePaymentMethod(string cardNumber, string expiryDate, string cvv);
}