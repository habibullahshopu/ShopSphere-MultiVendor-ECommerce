using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;
using ShopSphere.Models;
using ShopSphere.ViewModels;

namespace ShopSphere.Areas.Vendor.Controllers
{
    [Area("Vendor")]
    [Authorize(Roles = "Customer")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =========================================================
        // VENDOR DASHBOARD
        // GET: /Vendor/Dashboard
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var vendor = await _context.Vendors
                .AsNoTracking()
                .FirstOrDefaultAsync(v =>
                    v.UserId == user.Id &&
                    v.IsApproved &&
                    v.IsActive);

            if (vendor == null)
            {
                TempData["Error"] =
                    "You do not have an approved vendor account.";

                return RedirectToAction(
                    "Index",
                    "Home",
                    new { area = "Customer" });
            }

            // =====================================================
            // PAID VENDOR SALES
            // =====================================================

            var vendorOrderItems = await _context.OrderItems
                .AsNoTracking()
                .Where(oi =>
                    oi.Product != null &&
                    oi.Product.VendorId == vendor.Id &&
                    oi.Order != null &&
                    oi.Order.PaymentStatus == "Paid")
                .ToListAsync();

            // =====================================================
            // TOTAL ORDERS
            // =====================================================

            var totalOrders = vendorOrderItems
                .Select(oi => oi.OrderId)
                .Distinct()
                .Count();

            // =====================================================
            // TOTAL SALES
            // =====================================================

            var totalSales = vendorOrderItems
                .Sum(oi => oi.Subtotal);

            // =====================================================
            // PLATFORM COMMISSION
            // =====================================================

            var totalCommission =
                totalSales *
                vendor.CommissionRate /
                100m;

            // =====================================================
            // VENDOR NET EARNINGS
            // =====================================================

            var netEarnings =
                totalSales -
                totalCommission;

            // =====================================================
            // DASHBOARD VIEW MODEL
            // =====================================================

            var model = new VendorEarningsViewModel
            {
                StoreName =
                    vendor.StoreName,

                CommissionRate =
                    vendor.CommissionRate,

                TotalOrders =
                    totalOrders,

                TotalSales =
                    totalSales,

                TotalCommission =
                    totalCommission,

                NetEarnings =
                    netEarnings
            };

            // =====================================================
            // ADDITIONAL STORE INFORMATION
            // =====================================================

            ViewBag.StoreName =
                vendor.StoreName;

            ViewBag.CommissionRate =
                vendor.CommissionRate;

            ViewBag.ApprovedAt =
                vendor.ApprovedAt;

            return View(model);
        }
    }
}