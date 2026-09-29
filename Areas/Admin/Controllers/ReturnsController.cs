using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;
using ShopSphere.Models;
using ShopSphere.Services;
namespace ShopSphere.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ReturnsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public ReturnsController(
    ApplicationDbContext context,
    IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }
        // GET: /Admin/Returns
        public async Task<IActionResult> Index(
            string? search,
            string? status)
        {
            var query = _context.ReturnRequests
                .Include(r => r.Order)
                .Include(r => r.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(r =>
                    r.OrderId.ToString().Contains(search) ||
                    r.Reason.Contains(search) ||
                    (r.User != null &&
                     (r.User.FullName!.Contains(search) ||
                      r.User.Email!.Contains(search))));
            }

            if (!string.IsNullOrWhiteSpace(status) &&
                status != "All")
            {
                query = query.Where(r => r.Status == status);
            }

            var returns = await query
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Status = status ?? "All";

            ViewBag.PendingCount = await _context.ReturnRequests
                .CountAsync(r => r.Status == "Pending");

            ViewBag.ApprovedCount = await _context.ReturnRequests
                .CountAsync(r => r.Status == "Approved");

            ViewBag.RejectedCount = await _context.ReturnRequests
                .CountAsync(r => r.Status == "Rejected");

            ViewBag.CompletedCount = await _context.ReturnRequests
                .CountAsync(r => r.Status == "Completed");

            return View(returns);
        }

        // GET: /Admin/Returns/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var returnRequest = await _context.ReturnRequests
                .Include(r => r.Order)
                    .ThenInclude(o => o!.OrderItems)
                        .ThenInclude(oi => oi.Product)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (returnRequest == null)
                return NotFound();

            return View(returnRequest);
        }

        // POST: Approve
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(
            int id,
            decimal refundAmount,
            string? adminNote)
        {
            var returnRequest = await _context.ReturnRequests
                .Include(r => r.User)
    .Include(r => r.Order)
    .FirstOrDefaultAsync(r => r.Id == id);
            if (returnRequest == null)
                return NotFound();

            if (returnRequest.Status != "Pending")
            {
                TempData["ErrorMessage"] =
                    "Only pending return requests can be approved.";

                return RedirectToAction(nameof(Details), new { id });
            }

            if (refundAmount < 0)
            {
                TempData["ErrorMessage"] =
                    "Refund amount cannot be negative.";

                return RedirectToAction(nameof(Details), new { id });
            }

            if (returnRequest.Order != null &&
                refundAmount > returnRequest.Order.TotalAmount)
            {
                TempData["ErrorMessage"] =
                    "Refund amount cannot exceed the order total.";

                return RedirectToAction(nameof(Details), new { id });
            }

            returnRequest.Status = "Approved";
            returnRequest.RefundAmount = refundAmount;
            returnRequest.RefundStatus = "Pending";
            returnRequest.AdminNote =
                string.IsNullOrWhiteSpace(adminNote)
                    ? null
                    : adminNote.Trim();

            returnRequest.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            var customerEmail = returnRequest.User?.Email;

            if (!string.IsNullOrWhiteSpace(customerEmail))
            {
                var subject =
                    $"ShopSphere Return Request #{returnRequest.Id} Approved";

                var htmlMessage = $@"
        <div style='font-family:Arial,sans-serif;max-width:600px;margin:auto;padding:30px;border:1px solid #eee;border-radius:12px;'>
            <h2 style='color:#6f42c1;'>ShopSphere</h2>

            <h3>Return Request Approved ✅</h3>

            <p>Your return request has been approved by our admin team.</p>

            <div style='background:#f8f9fa;padding:20px;border-radius:10px;margin:20px 0;'>
                <p><strong>Return Request ID:</strong> #{returnRequest.Id}</p>
                <p><strong>Order ID:</strong> #{returnRequest.OrderId}</p>
                <p><strong>Refund Amount:</strong> ৳{returnRequest.RefundAmount:N2}</p>
                <p><strong>Refund Status:</strong> Pending</p>
            </div>

            <p>Your refund will be processed shortly.</p>

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
                catch (Exception ex)
                {
                    Console.WriteLine("RETURN APPROVAL EMAIL ERROR: " + ex.Message);
                }
            }

            TempData["SuccessMessage"] =
                $"Return request #{returnRequest.Id} has been approved.";

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Reject
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(
            int id,
            string? adminNote)
        {
            var returnRequest = await _context.ReturnRequests
                .Include(r => r.User)
    .Include(r => r.Order)
    .FirstOrDefaultAsync(r => r.Id == id);

            if (returnRequest == null)
                return NotFound();

            if (returnRequest.Status != "Pending")
            {
                TempData["ErrorMessage"] =
                    "Only pending return requests can be rejected.";

                return RedirectToAction(nameof(Details), new { id });
            }

            returnRequest.Status = "Rejected";
            returnRequest.RefundStatus = "Rejected";
            returnRequest.AdminNote =
                string.IsNullOrWhiteSpace(adminNote)
                    ? "Return request rejected by admin."
                    : adminNote.Trim();

            returnRequest.UpdatedAt = DateTime.UtcNow;
            returnRequest.ResolvedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            var customerEmail = returnRequest.User?.Email;

            if (!string.IsNullOrWhiteSpace(customerEmail))
            {
                var subject =
                    $"ShopSphere Return Request #{returnRequest.Id} Rejected";

                var htmlMessage = $@"
        <div style='font-family:Arial,sans-serif;max-width:600px;margin:auto;padding:30px;border:1px solid #eee;border-radius:12px;'>
            <h2 style='color:#6f42c1;'>ShopSphere</h2>

            <h3>Return Request Rejected ❌</h3>

            <p>We are sorry to inform you that your return request has been rejected by our admin team.</p>

            <div style='background:#f8f9fa;padding:20px;border-radius:10px;margin:20px 0;'>
                <p><strong>Return Request ID:</strong> #{returnRequest.Id}</p>
                <p><strong>Order ID:</strong> #{returnRequest.OrderId}</p>
                <p><strong>Refund Amount:</strong> ৳{returnRequest.RefundAmount:N2}</p>
                <p><strong>Refund Status:</strong> Rejected</p>
                <p><strong>Reason/Note:</strong> {returnRequest.AdminNote ?? "No additional note provided."}</p>
            </div>

            <p>If you have any questions, please contact ShopSphere support.</p>

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
                catch (Exception ex)
                {
                    Console.WriteLine("RETURN REJECTION EMAIL ERROR: " + ex.Message);
                }
            }
            TempData["SuccessMessage"] =
                $"Return request #{returnRequest.Id} has been rejected.";

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Mark refund as completed
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteRefund(int id)
        {
            var returnRequest = await _context.ReturnRequests
               .Include(r => r.User)
    .Include(r => r.Order)
    .FirstOrDefaultAsync(r => r.Id == id);

            if (returnRequest == null)
                return NotFound();

            if (returnRequest.Status != "Approved")
            {
                TempData["ErrorMessage"] =
                    "Only approved return requests can be refunded.";

                return RedirectToAction(nameof(Details), new { id });
            }

            returnRequest.RefundStatus = "Completed";
            returnRequest.Status = "Completed";
            returnRequest.UpdatedAt = DateTime.UtcNow;
            returnRequest.ResolvedAt = DateTime.UtcNow;
            if (returnRequest.Order != null)
            {
                returnRequest.Order.Status = "Refunded";
                returnRequest.Order.PaymentStatus = "Refunded";
            }
            await _context.SaveChangesAsync();
            var customerEmail = returnRequest.User?.Email;

            if (!string.IsNullOrWhiteSpace(customerEmail))
            {
                var subject =
                    $"ShopSphere Refund for Return Request #{returnRequest.Id} Completed";

                var htmlMessage = $@"
        <div style='font-family:Arial,sans-serif;max-width:600px;margin:auto;padding:30px;border:1px solid #eee;border-radius:12px;'>
            <h2 style='color:#6f42c1;'>ShopSphere</h2>

            <h3>Refund Completed 💰</h3>

            <p>Your refund has been marked as completed by our admin team.</p>

            <div style='background:#f8f9fa;padding:20px;border-radius:10px;margin:20px 0;'>
                <p><strong>Return Request ID:</strong> #{returnRequest.Id}</p>
                <p><strong>Order ID:</strong> #{returnRequest.OrderId}</p>
                <p><strong>Refund Amount:</strong> ৳{returnRequest.RefundAmount:N2}</p>
                <p><strong>Refund Status:</strong> Completed</p>
            </div>

            <p>Thank you for choosing <strong>ShopSphere</strong>.</p>

            <p style='margin-top:30px;'>
                We appreciate your trust in ShopSphere.
            </p>
        </div>";

                try
                {
                    await _emailService.SendEmailAsync(
                        customerEmail,
                        subject,
                        htmlMessage);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("REFUND COMPLETION EMAIL ERROR: " + ex.Message);
                }
            }
            TempData["SuccessMessage"] =
                $"Refund for return request #{returnRequest.Id} has been marked as completed.";

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}