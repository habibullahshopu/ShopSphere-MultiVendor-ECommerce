using Microsoft.AspNetCore.Mvc;
using ShopSphere.Data;
using ShopSphere.Models;

namespace ShopSphere.Controllers
{
    public class SupportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SupportController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(
            string name,
            string email,
            string subject,
            string message)
        {
            if (string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(subject) ||
                string.IsNullOrWhiteSpace(message))
            {
                TempData["ErrorMessage"] =
                    "Please fill in all fields before sending your message.";

                return View();
            }

            var ticket = new SupportTicket
            {
                Name = name.Trim(),
                Email = email.Trim(),
                Subject = subject.Trim(),
                Message = message.Trim(),
                Status = "Open",
                CreatedAt = DateTime.UtcNow
            };

            _context.SupportTickets.Add(ticket);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Your support request has been submitted successfully. Our team will contact you soon.";

            return RedirectToAction(nameof(Index));
        }
    }
}