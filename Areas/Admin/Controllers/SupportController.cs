using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;
using ShopSphere.Services;

namespace ShopSphere.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class SupportController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public SupportController(
            ApplicationDbContext context,
            IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()


        {
            var tickets = await _context.SupportTickets
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return View(tickets);
        }
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var ticket = await _context.SupportTickets
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null)
            {
                return NotFound();
            }

            return View(ticket);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(int id, string reply, string status)
        {
            var ticket = await _context.SupportTickets
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(reply))
            {
                TempData["ErrorMessage"] = "Please enter a reply.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var allowedStatuses = new[]
            {
        "Open",
        "In Progress",
        "Resolved",
        "Closed"
    };

            if (!allowedStatuses.Contains(status))
            {
                status = "Open";
            }

            ticket.AdminReply = reply.Trim();
            ticket.Status = status;
            ticket.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            try
            {
                var subject = $"ShopSphere Support Reply - Ticket #{ticket.Id}";

                var htmlMessage = $@"
        <div style='font-family:Arial,sans-serif;line-height:1.6;'>
            <h2 style='color:#2563eb;'>ShopSphere Support</h2>

            <p>Hello <strong>{ticket.Name}</strong>,</p>

            <p>
                Our support team has replied to your support request.
            </p>

            <div style='background:#f3f4f6;padding:20px;border-radius:10px;'>
                <strong>Subject:</strong> {ticket.Subject}
                <br/><br/>

                <strong>Admin Reply:</strong>
                <p style='white-space:pre-line;'>
                    {ticket.AdminReply}
                </p>
            </div>

            <p>
                <strong>Ticket #:</strong> {ticket.Id}
                <br/>
                <strong>Status:</strong> {ticket.Status}
            </p>

            <p>
                Thank you for contacting ShopSphere.
            </p>

            <p>
                Regards,<br/>
                <strong>ShopSphere Support Team</strong>
            </p>
        </div>";

                await _emailService.SendEmailAsync(
                    ticket.Email,
                    subject,
                    htmlMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SUPPORT EMAIL ERROR: {ex.Message}");
            }

            TempData["SuccessMessage"] =
                "Reply has been saved and the customer has been notified by email.";

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}