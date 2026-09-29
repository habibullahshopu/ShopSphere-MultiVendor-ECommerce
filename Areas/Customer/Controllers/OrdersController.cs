using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;
using ShopSphere.Models;

namespace ShopSphere.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize(Roles = "Customer")]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrdersController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =========================================================
        // GET: /Customer/Orders
        // =========================================================
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        // =========================================================
        // GET: /Customer/Orders/Details/5
      
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o =>
                    o.Id == id &&
                    o.UserId == userId);

            if (order == null)
                return NotFound();
            var existingReturnRequest = await _context.ReturnRequests
    .FirstOrDefaultAsync(r =>
        r.OrderId == id &&
        r.UserId == userId &&
        r.Status != "Rejected");

            ViewBag.HasReturnRequest = existingReturnRequest != null;
            ViewBag.ReturnRequestId = existingReturnRequest?.Id;
            return View(order);
        }
    }
}