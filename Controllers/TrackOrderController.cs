using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;

namespace ShopSphere.Controllers
{
    public class TrackOrderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TrackOrderController(ApplicationDbContext context)
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
        public async Task<IActionResult> Index(int orderId, string email)
        {
            if (orderId <= 0 || string.IsNullOrWhiteSpace(email))
            {
                TempData["ErrorMessage"] =
                    "Please enter a valid Order ID and email address.";

                return View();
            }

            email = email.Trim();

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o =>
                    o.Id == orderId &&
                    o.CustomerEmail == email);

            if (order == null)
            {
                TempData["ErrorMessage"] =
                    "No order was found with the provided Order ID and email.";

                return View();
            }

            return View("Result", order);
        }
    }
}