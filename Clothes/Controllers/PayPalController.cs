using Microsoft.AspNetCore.Mvc;
using PayPalService;

namespace Clothes.Controllers;

[ApiController]
[Route("api/paypal")]
public class PayPalController : ControllerBase
{
    private readonly IPayPalService _payPalService;

    public PayPalController(IPayPalService payPalService)
    {
        _payPalService = payPalService;
    }

    [HttpPost("create-order")]
    public async Task<IActionResult> CreateOrder([FromBody] PaymentRequest request)
    {
        try
        {
            var result = await _payPalService.CreateOrderWithPaymentMethod(
                request.Amount, request.Currency, request.PaymentMethodToken, request.ReturnUrl, request.CancelUrl);

            return string.IsNullOrEmpty(request.PaymentMethodToken) ?
                // For PayPal payments, return approval URL
                Ok(new { approvalUrl = result }) :
                // For card payments, return order ID
                Ok(new { orderId = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }


    [HttpPost("capture-order")]
    public async Task<IActionResult> CaptureOrder([FromBody] CaptureRequest request)
    {
        try
        {
            var result = await _payPalService.CaptureOrder(request.Token, request.PayerId);
            return Ok(new { result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
    
    [HttpPost("create-payment-method")]
    public async Task<IActionResult> CreatePaymentMethod([FromBody] PaymentMethodRequest request)
    {
        try
        {
            var paymentMethodToken = await _payPalService.CreatePaymentMethod(
                request.CardNumber, request.ExpiryDate, request.CVV);
            return Ok(new { paymentMethodToken });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

}

// PaymentRequest.cs
public class PaymentRequest
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string PaymentMethodToken { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = "http://localhost:4200/success";
    public string CancelUrl { get; set; } = "http://localhost:4200/cancel";
}

// CaptureRequest.cs
public class CaptureRequest
{
    public string Token { get; set; } = string.Empty;
    public string PayerId { get; set; } = string.Empty;
}

// PaymentMethodRequest.cs
public class PaymentMethodRequest
{
    public string CardNumber { get; set; } = string.Empty; // Card number (e.g., "4111111111111111")
    public string ExpiryDate { get; set; } = string.Empty; // Expiry month (e.g., "12")
    public string CVV { get; set; } = string.Empty; // CVV (e.g., "123")
}