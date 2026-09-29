using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;

namespace ShopSphere.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // GET: /Admin/Orders
        // =========================================================
        public async Task<IActionResult> Index(
            string? search,
            string? status,
            string? paymentStatus)
        {
            var query = _context.Orders
                .Include(o => o.OrderItems)
                .AsQueryable();

            // Search by Order ID, Customer Name or Email
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                if (int.TryParse(search, out var orderId))
                {
                    query = query.Where(o =>
                        o.Id == orderId ||
                        o.CustomerName.Contains(search) ||
                        o.CustomerEmail.Contains(search));
                }
                else
                {
                    query = query.Where(o =>
                        o.CustomerName.Contains(search) ||
                        o.CustomerEmail.Contains(search));
                }
            }

            // Order Status
            if (!string.IsNullOrWhiteSpace(status) &&
                status != "All")
            {
                query = query.Where(o =>
                    o.Status == status);
            }

            // Payment Status
            if (!string.IsNullOrWhiteSpace(paymentStatus) &&
                paymentStatus != "All")
            {
                query = query.Where(o =>
                    o.PaymentStatus == paymentStatus);
            }

            var orders = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Status = status ?? "All";
            ViewBag.PaymentStatus = paymentStatus ?? "All";

            return View(orders);
        }


        // =========================================================
        // GET: /Admin/Orders/Details/5
        // =========================================================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            // Payment is a separate entity/table.
            // Load it separately instead of using Order.Payment.
            var payment = await _context.Payments
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.OrderId == order.Id);

            ViewBag.Payment = payment;

            return View(order);
        }


        // =========================================================
        // POST: /Admin/Orders/UpdateStatus
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(
            int id,
            string status)
        {
            var allowedStatuses = new[]
            {
                "Pending",
                "Processing",
                "Shipped",
                "Delivered",
                "Cancelled",
                "Refunded"
            };

            if (!allowedStatuses.Contains(status))
            {
                TempData["ErrorMessage"] =
                    "Invalid order status.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            order.Status = status;


            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Order #{order.Id} status updated to {status}.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }


        // =========================================================
        // POST: /Admin/Orders/UpdatePaymentStatus
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePaymentStatus(
            int id,
            string paymentStatus)
        {
            var allowedPaymentStatuses = new[]
            {
                "Pending",
                "Paid",
                "Failed",
                "Refunded"
            };

            if (!allowedPaymentStatuses.Contains(paymentStatus))
            {
                TempData["ErrorMessage"] =
                    "Invalid payment status.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            order.PaymentStatus = paymentStatus;

            var payment = await _context.Payments
    .FirstOrDefaultAsync(p => p.OrderId == order.Id);

            if (payment != null)
            {
                payment.Status = paymentStatus;
            }
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Order #{order.Id} payment status updated to {paymentStatus}.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }
    }
}