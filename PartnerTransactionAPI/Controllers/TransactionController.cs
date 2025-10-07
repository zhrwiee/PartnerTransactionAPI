using Microsoft.AspNetCore.Mvc;
using PartnerTransactionAPI.Models;
using System.Text;
using System.Security.Cryptography;
using log4net;

namespace PartnerTransactionAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubmitTrxMessageController : ControllerBase
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(SubmitTrxMessageController));

        private static readonly Dictionary<string, string> AllowedPartners = new()
        {
            { "FAKEGOOGLE", "FAKEPASSWORD1234" },
            { "FAKEPEOPLE", "FAKEPASSWORD4578" }
        };

        [HttpPost]
        public IActionResult Post([FromBody] TransactionRequest req)
        {
            log.Info("===== New Transaction Request Received =====");

            // ✅ 1. Check mandatory fields
            var requiredFields = new Dictionary<string, string?>
            {
                { "partnerkey", req.PartnerKey },
                { "partnerrefno", req.PartnerRefNo },
                { "partnerpassword", req.PartnerPassword },
                { "timestamp", req.Timestamp },
                { "sig", req.Sig }
            };

            foreach (var field in requiredFields)
            {
                if (string.IsNullOrWhiteSpace(field.Value))
                {
                    log.Warn($"{field.Key} is missing.");
                    return BadRequest(new TransactionResponse
                    {
                        Result = 0,
                        ResultMessage = $"{field.Key} is required."
                    });
                }
            }

            // ✅ 2. Partner validation
            if (!AllowedPartners.ContainsKey(req.PartnerKey))
            {
                log.Warn($"Unauthorized partner key: {req.PartnerKey}");
                return Unauthorized(new TransactionResponse
                {
                    Result = 0,
                    ResultMessage = "Access Denied!"
                });
            }

            var plainPassword = AllowedPartners[req.PartnerKey];
            var expectedPasswordBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(plainPassword));

            if (req.PartnerPassword != expectedPasswordBase64)
            {
                log.Warn($"Invalid password for partner: {req.PartnerKey}");
                return Unauthorized(new TransactionResponse
                {
                    Result = 0,
                    ResultMessage = "Access Denied!"
                });
            }

            // ✅ 3. Timestamp validation (±5 minutes, using UTC)
            if (!DateTime.TryParse(req.Timestamp, out var reqTime))
            {
                log.Warn($"Invalid timestamp format received: {req.Timestamp}");
                return BadRequest(new TransactionResponse
                {
                    Result = 0,
                    ResultMessage = "Invalid timestamp format."
                });
            }

            var reqTimeUtc = reqTime.ToUniversalTime();
            var serverTimeUtc = DateTime.UtcNow;
            var diffMinutes = Math.Abs((serverTimeUtc - reqTimeUtc).TotalMinutes);

            if (diffMinutes > 5)
            {
                log.Warn($"Timestamp expired. Received: {reqTimeUtc:O}, Server: {serverTimeUtc:O}");
                return BadRequest(new TransactionResponse
                {
                    Result = 0,
                    ResultMessage = "Expired."
                });
            }

            // ✅ 4. Validate totalamount
            if (req.TotalAmount <= 0)
            {
                log.Warn("Invalid totalamount received.");
                return BadRequest(new TransactionResponse
                {
                    Result = 0,
                    ResultMessage = "totalamount must be positive."
                });
            }

            // ✅ 5. Validate item details
            long sumFromItems = 0;
            if (req.Items != null && req.Items.Count > 0)
            {
                foreach (var item in req.Items)
                {
                    if (string.IsNullOrWhiteSpace(item.PartnerItemRef))
                        return BadRequest(new TransactionResponse { Result = 0, ResultMessage = "partneritemref is required." });

                    if (string.IsNullOrWhiteSpace(item.Name))
                        return BadRequest(new TransactionResponse { Result = 0, ResultMessage = "name is required." });

                    if (item.Qty <= 0 || item.Qty > 5)
                        return BadRequest(new TransactionResponse { Result = 0, ResultMessage = "qty must be between 1 and 5." });

                    if (item.UnitPrice <= 0)
                        return BadRequest(new TransactionResponse { Result = 0, ResultMessage = "unitprice must be positive." });

                    sumFromItems += item.Qty * item.UnitPrice;
                }

                if (sumFromItems != req.TotalAmount)
                {
                    log.Warn($"Invalid Total Amount. Expected {sumFromItems}, Received {req.TotalAmount}");
                    return BadRequest(new TransactionResponse
                    {
                        Result = 0,
                        ResultMessage = "Invalid Total Amount."
                    });
                }
            }

            // ✅ 6. Signature validation
            if (!ValidateSignature(req))
            {
                log.Warn($"Signature validation failed for partner: {req.PartnerKey}");
                return Unauthorized(new TransactionResponse
                {
                    Result = 0,
                    ResultMessage = "Access Denied!"
                });
            }

            // ✅ Log success (encrypt password in log)
            string encryptedPassword = EncryptPassword(req.PartnerPassword);
            log.Info($"Transaction Success. Partner: {req.PartnerKey}, RefNo: {req.PartnerRefNo}, Amount: {req.TotalAmount}, Password(Enc): {encryptedPassword}");

            // ✅ SUCCESS
            return Ok(new TransactionResponse
            {
                Result = 1,
                TotalAmount = req.TotalAmount,
                TotalDiscount = 0,
                FinalAmount = req.TotalAmount
            });
        }

        private bool ValidateSignature(TransactionRequest req)
        {
            try
            {
                var date = DateTime.Parse(req.Timestamp).ToUniversalTime();
                var sigTimestamp = date.ToString("yyyyMMddHHmmss");

                var concatenated = $"{sigTimestamp}{req.PartnerKey}{req.PartnerRefNo}{req.TotalAmount}{req.PartnerPassword}";

                using var sha256 = SHA256.Create();
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(concatenated));
                var hex = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(hex));

                return base64 == req.Sig;
            }
            catch
            {
                return false;
            }
        }

        // ✅ Encrypt password before writing into log
        private string EncryptPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes).Substring(0, 10) + "..."; // shortened for log
        }
    }
}
