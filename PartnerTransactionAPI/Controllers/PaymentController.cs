using Microsoft.AspNetCore.Mvc;
using log4net;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private static readonly ILog _logger = LogManager.GetLogger(typeof(PaymentController));

    [HttpPost("validate")]
    public IActionResult ValidateRequest([FromBody] PaymentRequest request)
    {
        string requestJson = JsonSerializer.Serialize(request);

        // 🔒 Encrypt password (if any)
        if (!string.IsNullOrEmpty(request.Password))
        {
            request.Password = EncryptPassword(request.Password);
        }

        _logger.Info($"Incoming Request: {requestJson}");

        try
        {
            // --- Convert timestamp ke UTC ---
            DateTime requestUtc = request.Timestamp.Kind == DateTimeKind.Utc
                ? request.Timestamp
                : request.Timestamp.ToUniversalTime();

            DateTime serverUtc = DateTime.UtcNow;
            double diffMinutes = Math.Abs((serverUtc - requestUtc).TotalMinutes);

            if (diffMinutes > 5)
            {
                var expiredResponse = new
                {
                    result = 0,
                    message = "Expired. Provided timestamp exceed server time ±5min",
                    serverTime = serverUtc.ToString("o"),
                    requestTime = requestUtc.ToString("o"),
                    diffMinutes
                };

                _logger.Warn($"Expired request: {JsonSerializer.Serialize(expiredResponse)}");
                return Ok(expiredResponse);
            }

            // --- Calculate discount ---
            decimal totalAmount = request.TotalAmount;
            decimal discountPercent = 0;

            if (totalAmount >= 200 && totalAmount <= 500) discountPercent = 5;
            else if (totalAmount >= 501 && totalAmount <= 800) discountPercent = 7;
            else if (totalAmount >= 801 && totalAmount <= 1200) discountPercent = 10;
            else if (totalAmount > 1200) discountPercent = 15;

            if (IsPrime((int)totalAmount) && totalAmount > 500)
                discountPercent += 8;

            if (totalAmount > 900 && totalAmount % 10 == 5)
                discountPercent += 10;

            if (discountPercent > 20)
                discountPercent = 20;

            decimal totalDiscount = totalAmount * (discountPercent / 100);
            decimal finalAmount = totalAmount - totalDiscount;

            var response = new
            {
                result = 1,
                totalamount = totalAmount,
                totaldiscount = totalDiscount,
                finalamount = finalAmount,
                appliedDiscountPercent = discountPercent,
                serverTime = serverUtc.ToString("o"),
                requestTime = requestUtc.ToString("o"),
                diffMinutes
            };

            _logger.Info($"Response: {JsonSerializer.Serialize(response)}");
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.Error("Error processing request", ex);
            return StatusCode(500, new { result = 0, message = "Internal Server Error" });
        }
    }

    // --- Utility methods ---

    private bool IsPrime(int number)
    {
        if (number <= 1) return false;
        if (number == 2) return true;
        if (number % 2 == 0) return false;
        var boundary = (int)Math.Floor(Math.Sqrt(number));
        for (int i = 3; i <= boundary; i += 2)
            if (number % i == 0)
                return false;
        return true;
    }

    private string EncryptPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
}

public class PaymentRequest
{
    public string PartnerRefNo { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime Timestamp { get; set; }
    public string Password { get; set; } // optional field
}
