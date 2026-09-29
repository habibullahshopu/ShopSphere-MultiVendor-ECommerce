using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ShopSphere.Data;
using ShopSphere.Models;
using ShopSphere.Services;
using System.Globalization;

namespace ShopSphere.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize(Roles = "Customer")]
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISSLCOMMERZService _sslCommerzService;
        private readonly SSLCOMMERZSettings _settings;
        private readonly IEmailService _emailService;

        public PaymentController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ISSLCOMMERZService sslCommerzService,
            IOptions<SSLCOMMERZSettings> settings,
IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _sslCommerzService = sslCommerzService;
            _settings = settings.Value;
            _emailService = emailService;
        }

        // =========================================================
        // PAY / RETRY PAYMENT
        // GET: /Customer/Payment/Pay/5
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Pay(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o =>
                    o.Id == id &&
                    o.UserId == userId);

            if (order == null)
                return NotFound();

            // Already paid
            if (string.Equals(
                    order.PaymentStatus,
                    "Paid",
                    StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(
                    "Success",
                    "Checkout",
                    new
                    {
                        area = "Customer",
                        id = order.Id
                    });
            }

            // Find existing payment
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p =>
                    p.OrderId == order.Id);

            // ---------------------------------------------------------
            // Create payment record if missing
            // ---------------------------------------------------------

            if (payment == null)
            {
                var transactionId =
                    $"SS{order.Id}{Guid.NewGuid():N}";

                if (transactionId.Length > 30)
                {
                    transactionId =
                        transactionId.Substring(0, 30);
                }

                payment = new Payment
                {
                    OrderId = order.Id,
                    Amount = order.TotalAmount,
                    Currency = "BDT",
                    Status = "Pending",
                    GatewayStatus = "INITIATED",
                    TranId = transactionId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Payments.Add(payment);

                await _context.SaveChangesAsync();
            }

            // ---------------------------------------------------------
            // Do not allow retry on permanently completed payment
            // ---------------------------------------------------------

            if (string.Equals(
                    payment.Status,
                    "Paid",
                    StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(
                    "Success",
                    "Checkout",
                    new
                    {
                        area = "Customer",
                        id = order.Id
                    });
            }

            // ---------------------------------------------------------
            // If old session exists, reuse it
            // ---------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(payment.SessionKey))
            {
                var gatewayBase =
                    _settings.IsSandbox
                        ? "https://sandbox.sslcommerz.com"
                        : "https://securepay.sslcommerz.com";

                var gatewayUrl =
                    $"{gatewayBase}/gwprocess/v4/gw.php" +
                    $"?Q=PAY&SESSIONKEY=" +
                    Uri.EscapeDataString(payment.SessionKey);

                return Redirect(gatewayUrl);
            }

            // ---------------------------------------------------------
            // Create new payment session
            // ---------------------------------------------------------

            var sessionResult =
                await _sslCommerzService
                    .CreatePaymentSessionAsync(
                        order,
                        payment);

            if (!sessionResult.Success ||
                string.IsNullOrWhiteSpace(
                    sessionResult.GatewayPageUrl))
            {
                payment.Status = "Failed";

                payment.GatewayStatus =
                    "SESSION_FAILED";

                payment.GatewayResponse =
                    sessionResult.Message ??
                    "Unable to create SSLCOMMERZ payment session.";

                await _context.SaveChangesAsync();

                TempData["PaymentError"] =
                    sessionResult.Message ??
                    "Unable to connect to payment gateway.";

                return RedirectToAction(
                    "Index",
                    "Orders",
                    new
                    {
                        area = "Customer"
                    });
            }

            // Save gateway session
            payment.SessionKey =
                sessionResult.SessionKey;

            if (!string.IsNullOrWhiteSpace(
                    sessionResult.TransactionId))
            {
                payment.TranId =
                    sessionResult.TransactionId;
            }

            payment.Status = "Pending";

            payment.GatewayStatus =
                "SESSION_CREATED";

            payment.GatewayResponse =
                sessionResult.Message ??
                "Payment session created successfully.";

            await _context.SaveChangesAsync();

            return Redirect(
                sessionResult.GatewayPageUrl);
        }

        // =========================================================
        // SSLCOMMERZ SUCCESS
        // GET / POST:
        // /Customer/Payment/Success
        // =========================================================

        [AllowAnonymous]
        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> Success(
            string? tran_id,
            string? val_id,
            string? status)
        {
            if (string.IsNullOrWhiteSpace(tran_id))
            {
                TempData["PaymentError"] =
                    "Invalid payment transaction.";

                return RedirectToAction(
                    "Index",
                    "Home",
                    new
                    {
                        area = "Customer"
                    });
            }

            // ---------------------------------------------------------
            // Find payment
            // ---------------------------------------------------------

            var payment = await _context.Payments
                .Include(p => p.Order)
                .ThenInclude(o => o!.OrderItems)
                .FirstOrDefaultAsync(p =>
                    p.TranId == tran_id);

            if (payment == null ||
                payment.Order == null)
            {
                return BadRequest(
                    "Payment transaction was not found.");
            }

            // ---------------------------------------------------------
            // Already paid
            // ---------------------------------------------------------

            if (string.Equals(
                    payment.Status,
                    "Paid",
                    StringComparison.OrdinalIgnoreCase) &&
                string.Equals(
                    payment.Order.PaymentStatus,
                    "Paid",
                    StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(
                    "Success",
                    "Checkout",
                    new
                    {
                        area = "Customer",
                        id = payment.OrderId
                    });
            }

            // ---------------------------------------------------------
            // Prevent a failed/cancelled payment from being
            // accidentally converted into Paid
            // ---------------------------------------------------------

            if (string.Equals(
                    payment.Status,
                    "Failed",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    payment.Status,
                    "Cancelled",
                    StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(
                    "PaymentFailed",
                    new
                    {
                        id = payment.OrderId
                    });
            }

            // ---------------------------------------------------------
            // Validation ID required
            // ---------------------------------------------------------

            if (string.IsNullOrWhiteSpace(val_id))
            {
                payment.Status = "Failed";

                payment.GatewayStatus =
                    status ?? "INVALID";

                payment.GatewayResponse =
                    "SSLCOMMERZ did not return a validation ID.";

                payment.Order.PaymentStatus =
                    "Failed";

                payment.Order.Status =
                    "Cancelled";

                await RestoreReservedStockAsync(
                    payment.Order);

                await _context.SaveChangesAsync();

                return RedirectToAction(
                    "PaymentFailed",
                    new
                    {
                        id = payment.OrderId
                    });
            }

            // =========================================================
            // VALIDATE WITH SSLCOMMERZ
            // =========================================================

            var validation =
                await _sslCommerzService
                    .ValidateTransactionAsync(
                        val_id);

            if (validation == null)
            {
                payment.GatewayStatus =
                    "VALIDATION_FAILED";

                payment.GatewayResponse =
                    "Unable to validate transaction with SSLCOMMERZ.";

                await _context.SaveChangesAsync();

                return RedirectToAction(
                    "PaymentFailed",
                    new
                    {
                        id = payment.OrderId
                    });
            }

            // =========================================================
            // SECURITY CHECKS
            // =========================================================

            var tranMatches =
                string.Equals(
                    validation.TranId,
                    payment.TranId,
                    StringComparison.OrdinalIgnoreCase);

            var amountMatches =
                Math.Abs(
                    validation.Amount -
                    payment.Amount) < 0.01m;

            var currencyMatches =
                string.Equals(
                    validation.Currency,
                    payment.Currency,
                    StringComparison.OrdinalIgnoreCase);

            var statusValid =
                string.Equals(
                    validation.Status,
                    "VALID",
                    StringComparison.OrdinalIgnoreCase)
                ||
                string.Equals(
                    validation.Status,
                    "VALIDATED",
                    StringComparison.OrdinalIgnoreCase);

            // ---------------------------------------------------------
            // Reject invalid transaction
            // ---------------------------------------------------------

            if (!tranMatches ||
                !amountMatches ||
                !currencyMatches ||
                !statusValid)
            {
                payment.Status = "Failed";

                payment.GatewayStatus =
                    validation.Status ?? "INVALID";

                payment.GatewayResponse =
                    "Payment validation failed. " +
                    "Transaction, amount, currency or status did not match.";

                payment.Order.PaymentStatus =
                    "Failed";

                payment.Order.Status =
                    "Cancelled";

                await RestoreReservedStockAsync(
                    payment.Order);

                await _context.SaveChangesAsync();

                return RedirectToAction(
                    "PaymentFailed",
                    new
                    {
                        id = payment.OrderId
                    });
            }

            // =========================================================
            // SUCCESSFUL PAYMENT
            // =========================================================

            payment.Status = "Paid";

            payment.ValId =
                validation.ValId;

            payment.BankTranId =
                validation.BankTranId;

            payment.CardType =
                validation.CardType;

            payment.GatewayStatus =
                validation.Status;

            payment.GatewayResponse =
                "Transaction successfully validated.";

            payment.PaidAt =
                DateTime.UtcNow;

            payment.Order.PaymentStatus =
                "Paid";

            payment.Order.Status =
                "Confirmed";

            await _context.SaveChangesAsync();

            // ---------------------------------------------------------
            // Redirect to order success page
            // ---------------------------------------------------------

            return RedirectToAction(
                "Success",
                "Checkout",
                new
                {
                    area = "Customer",
                    id = payment.OrderId
                });
        }

        // =========================================================
        // SSLCOMMERZ FAIL
        // GET: /Customer/Payment/Fail
        // =========================================================

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Fail(
            string? tran_id,
            string? status)
        {
            var payment =
                await FindPaymentByTransactionAsync(
                    tran_id);

            if (payment == null)
            {
                return RedirectToAction(
                    "Index",
                    "Home",
                    new
                    {
                        area = "Customer"
                    });
            }

            // If already paid, never overwrite it
            if (string.Equals(
                    payment.Status,
                    "Paid",
                    StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(
                    "Success",
                    "Checkout",
                    new
                    {
                        area = "Customer",
                        id = payment.OrderId
                    });
            }

            // Only process Pending payment
            if (string.Equals(
                    payment.Status,
                    "Pending",
                    StringComparison.OrdinalIgnoreCase))
            {
                payment.Status = "Failed";

                payment.GatewayStatus =
                    status ?? "FAILED";

                payment.GatewayResponse =
                    "Customer payment failed.";

                payment.Order!.PaymentStatus =
                    "Failed";

                payment.Order.Status =
                    "Cancelled";

                await RestoreReservedStockAsync(
                    payment.Order);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(
                "PaymentFailed",
                new
                {
                    id = payment.OrderId
                });
        }

        // =========================================================
        // SSLCOMMERZ CANCEL
        // GET: /Customer/Payment/Cancel
        // =========================================================

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Cancel(
            string? tran_id,
            string? status)
        {
            var payment =
                await FindPaymentByTransactionAsync(
                    tran_id);

            if (payment == null)
            {
                return RedirectToAction(
                    "Index",
                    "Home",
                    new
                    {
                        area = "Customer"
                    });
            }

            // Already paid - never overwrite
            if (string.Equals(
                    payment.Status,
                    "Paid",
                    StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(
                    "Success",
                    "Checkout",
                    new
                    {
                        area = "Customer",
                        id = payment.OrderId
                    });
            }

            // Only process Pending payment
            if (string.Equals(
                    payment.Status,
                    "Pending",
                    StringComparison.OrdinalIgnoreCase))
            {
                payment.Status = "Cancelled";

                payment.GatewayStatus =
                    status ?? "CANCELLED";

                payment.GatewayResponse =
                    "Customer cancelled the payment.";

                payment.Order!.PaymentStatus =
                    "Cancelled";

                payment.Order.Status =
                    "Cancelled";

                await RestoreReservedStockAsync(
                    payment.Order);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(
                "PaymentCancelled",
                new
                {
                    id = payment.OrderId
                });
        }

        // =========================================================
        // SSLCOMMERZ IPN
        //
        // POST:
        // /Customer/Payment/IPN
        //
        // IMPORTANT:
        // SSLCOMMERZ server calls this endpoint.
        // No customer login/session is required.
        // =========================================================

        [AllowAnonymous]
        [HttpPost("/Customer/Payment/IPN")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> IPN()
        {

            Console.WriteLine("========== SHOPSPHERE IPN HIT ==========");
            // ---------------------------------------------------------
            // Check content type
            // ---------------------------------------------------------

            if (!Request.HasFormContentType)
            {
                return BadRequest(
                    "Invalid IPN content type.");
            }

            var form =
                await Request.ReadFormAsync();

            // ---------------------------------------------------------
            // Read SSLCOMMERZ parameters
            // ---------------------------------------------------------

            var tranId =
                form["tran_id"].ToString().Trim();

            var valId =
                form["val_id"].ToString().Trim();

            var gatewayStatus =
                form["status"].ToString().Trim();

            var amountText =
                form["amount"].ToString().Trim();

            var currency =
                form["currency"].ToString().Trim();

            // ---------------------------------------------------------
            // Transaction ID required
            // ---------------------------------------------------------

            if (string.IsNullOrWhiteSpace(tranId))
            {
                return BadRequest(
                    "Transaction ID is required.");
            }

            // ---------------------------------------------------------
            // Find payment
            // ---------------------------------------------------------

            var payment =
                await _context.Payments
                    .Include(p => p.Order)
                    .ThenInclude(o => o!.OrderItems)
                    .FirstOrDefaultAsync(p =>
                        p.TranId == tranId);

            // ---------------------------------------------------------
            // Unknown transaction
            //
            // IMPORTANT:
            // Do not expose database details.
            // Return 200 so gateway does not repeatedly retry
            // a completely unknown transaction.
            // ---------------------------------------------------------

            if (payment == null ||
                payment.Order == null)
            {
                return Ok(new
                {
                    success = false,
                    message = "IPN received."
                });
            }

            // =========================================================
            // ALREADY PAID
            // =========================================================

            if (string.Equals(
                    payment.Status,
                    "Paid",
                    StringComparison.OrdinalIgnoreCase))
            {
                return Ok(new
                {
                    success = true,
                    message = "Payment already processed."
                });
            }

            // =========================================================
            // SUCCESSFUL IPN
            // =========================================================

            if (string.Equals(
                    gatewayStatus,
                    "VALID",
                    StringComparison.OrdinalIgnoreCase))
            {
                // -----------------------------------------------------
                // Parse amount
                // -----------------------------------------------------

                var amountParsed =
                    decimal.TryParse(
                        amountText,
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out var incomingAmount);

                if (!amountParsed)
                {
                    payment.GatewayStatus =
                        "IPN_INVALID_AMOUNT";

                    payment.GatewayResponse =
                        "Unable to parse IPN amount.";

                    await _context.SaveChangesAsync();

                    return BadRequest(
                        "Invalid IPN amount.");
                }

                // -----------------------------------------------------
                // Check amount
                // -----------------------------------------------------

                var amountMatches =
                    Math.Abs(
                        incomingAmount -
                        payment.Amount) < 0.01m;

                // -----------------------------------------------------
                // Check currency
                // -----------------------------------------------------

                var currencyMatches =
                    string.Equals(
                        currency,
                        payment.Currency,
                        StringComparison.OrdinalIgnoreCase);

                // -----------------------------------------------------
                // Validation ID required
                // -----------------------------------------------------

                if (!amountMatches ||
                    !currencyMatches ||
                    string.IsNullOrWhiteSpace(valId))
                {
                    payment.GatewayStatus =
                        "IPN_VALIDATION_FAILED";

                    payment.GatewayResponse =
                        "IPN amount, currency or validation ID did not match.";

                    await _context.SaveChangesAsync();

                    return BadRequest(
                        "IPN validation failed.");
                }

                // -----------------------------------------------------
                // Server-side validation
                // -----------------------------------------------------

                var validation =
                    await _sslCommerzService
                        .ValidateTransactionAsync(
                            valId);

                if (validation == null)
                {
                    payment.GatewayStatus =
                        "VALIDATION_FAILED";

                    payment.GatewayResponse =
                        "SSLCOMMERZ validation API returned no valid response.";

                    await _context.SaveChangesAsync();

                    return BadRequest(
                        "Transaction validation failed.");
                }

                // -----------------------------------------------------
                // Validate transaction ID
                // -----------------------------------------------------

                var tranMatches =
                    string.Equals(
                        validation.TranId,
                        payment.TranId,
                        StringComparison.OrdinalIgnoreCase);

                // -----------------------------------------------------
                // Validate amount
                // -----------------------------------------------------

                var validatedAmountMatches =
                    Math.Abs(
                        validation.Amount -
                        payment.Amount) < 0.01m;

                // -----------------------------------------------------
                // Validate currency
                // -----------------------------------------------------

                var validatedCurrencyMatches =
                    string.Equals(
                        validation.Currency,
                        payment.Currency,
                        StringComparison.OrdinalIgnoreCase);

                // -----------------------------------------------------
                // Validate status
                // -----------------------------------------------------

                var validStatus =
                    string.Equals(
                        validation.Status,
                        "VALID",
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    string.Equals(
                        validation.Status,
                        "VALIDATED",
                        StringComparison.OrdinalIgnoreCase);

                // -----------------------------------------------------
                // Reject if anything does not match
                // -----------------------------------------------------

                if (!tranMatches ||
                    !validatedAmountMatches ||
                    !validatedCurrencyMatches ||
                    !validStatus)
                {
                    payment.GatewayStatus =
                        "VALIDATION_REJECTED";

                    payment.GatewayResponse =
                        "SSLCOMMERZ validation did not match local order.";

                    await _context.SaveChangesAsync();

                    return BadRequest(
                        "Transaction validation rejected.");
                }

                // -----------------------------------------------------
                // SUCCESS
                // -----------------------------------------------------

                payment.Status = "Paid";

                payment.ValId =
                    validation.ValId;

                payment.BankTranId =
                    validation.BankTranId;

                payment.CardType =
                    validation.CardType;

                payment.GatewayStatus =
                    validation.Status;

                payment.GatewayResponse =
                    "Payment successfully validated through IPN.";

                payment.PaidAt =
                    DateTime.UtcNow;

                payment.Order.PaymentStatus =
                    "Paid";

                payment.Order.Status =
                    "Confirmed";

                await _context.SaveChangesAsync();
                var customerEmail = payment.Order.CustomerEmail;

                if (!string.IsNullOrWhiteSpace(customerEmail))
                {
                    var subject = $"ShopSphere Order #{payment.OrderId} Confirmed";

                    var htmlMessage = $@"
        <div style='font-family:Arial,sans-serif;max-width:600px;margin:auto;padding:30px;border:1px solid #eee;border-radius:12px;'>
            <h2 style='color:#6f42c1;'>ShopSphere</h2>

            <h3>Order Confirmed! 🎉</h3>

            <p>Thank you for shopping with ShopSphere.</p>

            <p>Your payment has been successfully verified.</p>

            <div style='background:#f8f9fa;padding:20px;border-radius:10px;margin:20px 0;'>
                <p><strong>Order ID:</strong> #{payment.OrderId}</p>
                <p><strong>Amount Paid:</strong> ৳{payment.Amount:N2}</p>
                <p><strong>Payment Status:</strong> Paid</p>
                <p><strong>Transaction ID:</strong> {payment.TranId}</p>
            </div>

            <p>Your order is now confirmed and will be processed shortly.</p>

            <p style='margin-top:30px;'>
                Thank you for choosing <strong>ShopSphere</strong>.
            </p>
        </div>";

                    try
                    {
                        await _emailService.SendEmailAsync(
                            customerEmail,
                            subject,
                            htmlMessage);
                    }
                    catch
                    {
                        // Email failure should not break a successful payment.
                    }
                }

                return Ok(new
                {
                    success = true,
                    message = "Payment successfully processed."
                });
            }

            // =========================================================
            // FAILED
            // =========================================================

            if (string.Equals(
                    gatewayStatus,
                    "FAILED",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    gatewayStatus,
                    "EXPIRED",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    gatewayStatus,
                    "UNATTEMPTED",
                    StringComparison.OrdinalIgnoreCase))
            {
                // Only restore stock when changing from Pending.
                // This prevents duplicate stock restoration if
                // SSLCOMMERZ sends the same IPN more than once.

                if (string.Equals(
                        payment.Status,
                        "Pending",
                        StringComparison.OrdinalIgnoreCase))
                {
                    payment.Status = "Failed";

                    payment.GatewayStatus =
                        gatewayStatus;

                    payment.GatewayResponse =
                        $"SSLCOMMERZ IPN status: {gatewayStatus}";

                    payment.Order.PaymentStatus =
                        "Failed";

                    payment.Order.Status =
                        "Cancelled";

                    await RestoreReservedStockAsync(
                        payment.Order);

                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    success = true,
                    message = "Failed payment processed."
                });
            }

            // =========================================================
            // CANCELLED
            // =========================================================

            if (string.Equals(
                    gatewayStatus,
                    "CANCELLED",
                    StringComparison.OrdinalIgnoreCase))
            {
                // Only restore stock when changing from Pending.
                if (string.Equals(
                        payment.Status,
                        "Pending",
                        StringComparison.OrdinalIgnoreCase))
                {
                    payment.Status = "Cancelled";

                    payment.GatewayStatus =
                        gatewayStatus;

                    payment.GatewayResponse =
                        "SSLCOMMERZ payment was cancelled.";

                    payment.Order.PaymentStatus =
                        "Cancelled";

                    payment.Order.Status =
                        "Cancelled";

                    await RestoreReservedStockAsync(
                        payment.Order);

                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    success = true,
                    message = "Cancelled payment processed."
                });
            }

            // =========================================================
            // OTHER / UNKNOWN STATUS
            // =========================================================

            payment.GatewayStatus =
                gatewayStatus;

            payment.GatewayResponse =
                $"SSLCOMMERZ IPN status received: {gatewayStatus}";

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "IPN received."
            });
        }

        // =========================================================
        // PAYMENT FAILED PAGE
        // GET:
        // /Customer/Payment/PaymentFailed/5
        // =========================================================

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> PaymentFailed(
            int id)
        {
            var order =
                await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o =>
                        o.Id == id);

            if (order == null)
                return NotFound();

            return View(order);
        }

        // =========================================================
        // PAYMENT CANCELLED PAGE
        // GET:
        // /Customer/Payment/PaymentCancelled/5
        // =========================================================

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> PaymentCancelled(
            int id)
        {
            var order =
                await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o =>
                        o.Id == id);

            if (order == null)
                return NotFound();

            return View(order);
        }

        // =========================================================
        // FIND PAYMENT BY TRANSACTION ID
        // =========================================================

        private async Task<Payment?>
            FindPaymentByTransactionAsync(
                string? tranId)
        {
            if (string.IsNullOrWhiteSpace(tranId))
                return null;

            return await _context.Payments
                .Include(p => p.Order)
                .ThenInclude(o => o!.OrderItems)
                .FirstOrDefaultAsync(p =>
                    p.TranId == tranId);
        }

        // =========================================================
        // RESTORE RESERVED STOCK
        // =========================================================

        private async Task RestoreReservedStockAsync(
            Order order)
        {
            var orderItems =
                await _context.OrderItems
                    .Where(oi =>
                        oi.OrderId == order.Id)
                    .ToListAsync();

            foreach (var orderItem in orderItems)
            {
                var product =
                    await _context.Products
                        .FirstOrDefaultAsync(p =>
                            p.Id == orderItem.ProductId);

                if (product != null)
                {
                    product.StockQuantity +=
                        orderItem.Quantity;
                }
            }
        }
    }
}