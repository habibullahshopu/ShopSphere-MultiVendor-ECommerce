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
    public class EarningsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EarningsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

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
                .FirstOrDefaultAsync(v => v.UserId == user.Id);

            if (vendor == null)
            {
                TempData["Error"] =
                    "You do not have a vendor account.";

                return RedirectToAction(
                    "Index",
                    "Home",
                    new { area = "Customer" });
            }

            if (!vendor.IsApproved || !vendor.IsActive)
            {
                TempData["Error"] =
                    "Your vendor account is not approved or is currently inactive.";

                return RedirectToAction(
                    "Index",
                    "Home",
                    new { area = "Customer" });
            }

            var vendorOrderItems = await _context.OrderItems
                .AsNoTracking()
                .Include(oi => oi.Order)
                .Include(oi => oi.Product)
                .Where(oi =>
                    oi.Product != null &&
                    oi.Product.VendorId == vendor.Id &&
                    oi.Order != null &&
                    oi.Order.PaymentStatus == "Paid")
                .OrderByDescending(oi => oi.Order!.CreatedAt)
                .ToListAsync();

            var earnings = vendorOrderItems
                .Select(oi =>
                {
                    var commissionAmount =
                        oi.Subtotal * vendor.CommissionRate / 100m;

                    var netEarnings =
                        oi.Subtotal - commissionAmount;

                    return new VendorEarningItemViewModel
                    {
                        OrderId = oi.OrderId,

                        OrderDate = oi.Order!.CreatedAt,

                        ProductName = oi.ProductName,

                        Quantity = oi.Quantity,

                        UnitPrice = oi.UnitPrice,

                        Subtotal = oi.Subtotal,

                        CommissionAmount = commissionAmount,

                        NetEarnings = netEarnings,

                        PaymentStatus = oi.Order.PaymentStatus
                    };
                })
                .ToList();

            var model = new VendorEarningsHistoryViewModel
            {
                StoreName = vendor.StoreName,

                CommissionRate = vendor.CommissionRate,

                TotalOrders = earnings
                    .Select(e => e.OrderId)
                    .Distinct()
                    .Count(),

                TotalSales = earnings
                    .Sum(e => e.Subtotal),

                TotalCommission = earnings
                    .Sum(e => e.CommissionAmount),

                NetEarnings = earnings
                    .Sum(e => e.NetEarnings),

                Earnings = earnings
            };

            return View(model);
        }
    }
}