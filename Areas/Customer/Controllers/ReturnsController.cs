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
    public class ReturnsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReturnsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Customer return request list
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var returns = await _context.ReturnRequests
                .Include(r => r.Order)
                .Where(r => r.UserId == user.Id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(returns);
        }

        // Return request form
        [HttpGet]
        public async Task<IActionResult> Create(int orderId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var order = await _context.Orders
                .FirstOrDefaultAsync(o =>
                    o.Id == orderId &&
                    o.UserId == user.Id);

            if (order == null)
                return NotFound();

            // Prevent duplicate active return request
            var existingRequest = await _context.ReturnRequests
                .AnyAsync(r =>
                    r.OrderId == orderId &&
                    r.UserId == user.Id &&
                    r.Status != "Rejected");

            if (existingRequest)
            {
                TempData["ErrorMessage"] =
                    "A return request already exists for this order.";

                return RedirectToAction(
                    "Details",
                    "Orders",
                    new { area = "Customer", id = orderId });
            }

            var model = new ReturnRequest
            {
                OrderId = orderId,
                UserId = user.Id,
                RefundAmount = order.TotalAmount,
                Status = "Pending",
                RefundStatus = "Pending"
            };

            return View(model);
        }

        // Submit return request
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReturnRequest model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            ModelState.Remove(nameof(ReturnRequest.UserId));
            ModelState.Remove(nameof(ReturnRequest.Order));
            ModelState.Remove(nameof(ReturnRequest.Status));
            ModelState.Remove(nameof(ReturnRequest.RefundStatus));
            ModelState.Remove(nameof(ReturnRequest.RefundAmount));

            if (!ModelState.IsValid)
                return View(model);

            var order = await _context.Orders
                .FirstOrDefaultAsync(o =>
                    o.Id == model.OrderId &&
                    o.UserId == user.Id);

            if (order == null)
                return NotFound();

            // Return request should only be allowed for completed/paid orders
            if (order.PaymentStatus != "Paid")
            {
                TempData["ErrorMessage"] =
                    "A return request can only be submitted for a paid order.";

                return RedirectToAction(
                    "Details",
                    "Orders",
                    new { area = "Customer", id = order.Id });
            }

            var existingRequest = await _context.ReturnRequests
                .AnyAsync(r =>
                    r.OrderId == order.Id &&
                    r.UserId == user.Id &&
                    r.Status != "Rejected");

            if (existingRequest)
            {
                TempData["ErrorMessage"] =
                    "A return request already exists for this order.";

                return RedirectToAction(
                    "Details",
                    "Orders",
                    new { area = "Customer", id = order.Id });
            }

            model.UserId = user.Id;
            model.Status = "Pending";
            model.RefundStatus = "Pending";
            model.RefundAmount = order.TotalAmount;
            model.CreatedAt = DateTime.UtcNow;
            model.UpdatedAt = null;
            model.ResolvedAt = null;

            _context.ReturnRequests.Add(model);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Return request for Order #{order.Id} has been submitted successfully.";

            return RedirectToAction(nameof(Index));
        }

        // Customer return details
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var returnRequest = await _context.ReturnRequests
                .Include(r => r.Order)
                .FirstOrDefaultAsync(r =>
                    r.Id == id &&
                    r.UserId == user.Id);

            if (returnRequest == null)
                return NotFound();

            return View(returnRequest);
        }
    }
}