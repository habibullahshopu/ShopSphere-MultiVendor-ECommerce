using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;
using ShopSphere.Models;
using ShopSphere.ViewModels;
using VendorModel = ShopSphere.Models.Vendor;

namespace ShopSphere.Areas.Vendor.Controllers
{
    [Area("Vendor")]
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
        // VENDOR ORDERS
        // GET: /Vendor/Orders
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var vendor = await GetCurrentVendorAsync();

            if (vendor == null)
            {
                return RedirectToVendorDashboard();
            }

            var orderItems = await _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Product)
                .Where(oi =>
                    oi.Order != null &&
                    oi.Order.PaymentStatus == "Paid" &&
                    oi.Product != null &&
                    oi.Product.VendorId == vendor.Id)
                .OrderByDescending(oi => oi.Order!.CreatedAt)
                .ToListAsync();

            var orders = orderItems
                .GroupBy(oi => oi.OrderId)
                .Select(group =>
                {
                    var order = group.First().Order!;

                    var vendorSubtotal = group.Sum(oi =>
                        oi.UnitPrice * oi.Quantity);

                    var commissionAmount =
                        vendorSubtotal *
                        vendor.CommissionRate / 100m;

                    var vendorEarnings =
                        vendorSubtotal -
                        commissionAmount;

                    return new VendorOrderViewModel
                    {
                        OrderId = order.Id,

                        OrderDate = order.CreatedAt,

                        CustomerName =
                            order.CustomerName,

                        CustomerEmail =
                            order.CustomerEmail,

                        ShippingAddress =
                            order.ShippingAddress,

                        City =
                            order.City,

                        OrderStatus =
                            order.Status,

                        PaymentStatus =
                            order.PaymentStatus,

                        VendorSubtotal =
                            vendorSubtotal,

                        CommissionAmount =
                            commissionAmount,

                        VendorEarnings =
                            vendorEarnings,

                        Items = group
                            .Select(oi => new VendorOrderItemViewModel
                            {
                                ProductId =
                                    oi.ProductId,

                                ProductName =
                                    oi.Product != null
                                        ? oi.Product.Name
                                        : "Product",

                                Quantity =
                                    oi.Quantity,

                                UnitPrice =
                                    oi.UnitPrice,

                                TotalPrice =
                                    oi.UnitPrice * oi.Quantity
                            })
                            .ToList()
                    };
                })
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            return View(orders);
        }


        // =========================================================
        // ORDER DETAILS
        // GET: /Vendor/Orders/Details/5
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var vendor = await GetCurrentVendorAsync();

            if (vendor == null)
            {
                return RedirectToVendorDashboard();
            }

            var orderItems = await _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Product)
                .Where(oi =>
                    oi.OrderId == id &&
                    oi.Order != null &&
                    oi.Order.PaymentStatus == "Paid" &&
                    oi.Product != null &&
                    oi.Product.VendorId == vendor.Id)
                .ToListAsync();

            if (!orderItems.Any())
            {
                return NotFound();
            }

            var order = orderItems.First().Order;

            if (order == null)
            {
                return NotFound();
            }

            var vendorSubtotal = orderItems.Sum(oi =>
                oi.UnitPrice * oi.Quantity);

            var commissionAmount =
                vendorSubtotal *
                vendor.CommissionRate / 100m;

            var vendorEarnings =
                vendorSubtotal -
                commissionAmount;

            var model = new VendorOrderViewModel
            {
                OrderId =
                    order.Id,

                OrderDate =
                    order.CreatedAt,

                CustomerName =
                    order.CustomerName,

                CustomerEmail =
                    order.CustomerEmail,

                ShippingAddress =
                    order.ShippingAddress,

                City =
                    order.City,

                OrderStatus =
                    order.Status,

                PaymentStatus =
                    order.PaymentStatus,

                VendorSubtotal =
                    vendorSubtotal,

                CommissionAmount =
                    commissionAmount,

                VendorEarnings =
                    vendorEarnings,

                Items = orderItems
                    .Select(oi => new VendorOrderItemViewModel
                    {
                        ProductId =
                            oi.ProductId,

                        ProductName =
                            oi.Product != null
                                ? oi.Product.Name
                                : "Product",

                        Quantity =
                            oi.Quantity,

                        UnitPrice =
                            oi.UnitPrice,

                        TotalPrice =
                            oi.UnitPrice * oi.Quantity
                    })
                    .ToList()
            };

            return View(model);
        }


        // =========================================================
        // UPDATE ORDER STATUS
        // POST: /Vendor/Orders/UpdateStatus
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(
            int id,
            string status)
        {
            var vendor = await GetCurrentVendorAsync();

            if (vendor == null)
            {
                return RedirectToVendorDashboard();
            }

            if (string.IsNullOrWhiteSpace(status))
            {
                TempData["Error"] =
                    "Please select an order status.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            var allowedStatuses = new[]
            {
                "Confirmed",
                "Processing",
                "Shipped",
                "Delivered"
            };

            if (!allowedStatuses.Contains(status))
            {
                TempData["Error"] =
                    "Invalid order status.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            // Only paid orders can be processed by vendor.
            if (order.PaymentStatus != "Paid")
            {
                TempData["Error"] =
                    "Only paid orders can be processed.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            // Make sure this order contains at least
            // one product belonging to the current vendor.
            var hasVendorProduct = order.OrderItems
                .Any(oi =>
                    oi.Product != null &&
                    oi.Product.VendorId == vendor.Id);

            if (!hasVendorProduct)
            {
                return Forbid();
            }

            // A cancelled order cannot be updated.
            if (order.Status == "Cancelled")
            {
                TempData["Error"] =
                    "A cancelled order cannot be updated.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            order.Status = status;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Order #{order.Id} status updated to {status}.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }


        // =========================================================
        // CURRENT VENDOR
        // =========================================================

        private async Task<VendorModel?> GetCurrentVendorAsync()
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return null;
            }

            var vendor =
                await _context.Vendors
                    .FirstOrDefaultAsync(v =>
                        v.UserId == user.Id &&
                        v.IsApproved &&
                        v.IsActive);

            return vendor;
        }


        // =========================================================
        // REDIRECT TO VENDOR DASHBOARD
        // =========================================================

        private IActionResult RedirectToVendorDashboard()
        {
            return RedirectToAction(
                "Index",
                "Dashboard",
                new
                {
                    area = "Vendor"
                });
        }
    }
}