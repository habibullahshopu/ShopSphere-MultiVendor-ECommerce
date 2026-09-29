using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;
using ShopSphere.Models;

namespace ShopSphere.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CouponsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CouponsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Coupons
        public async Task<IActionResult> Index(
            string? search,
            string? discountType,
            string? status)
        {
            var query = _context.Coupons
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(c =>
                    c.Code.Contains(search) ||
                    (c.Description != null &&
                     c.Description.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(discountType) &&
                discountType != "All")
            {
                query = query.Where(c =>
                    c.DiscountType == discountType);
            }

            if (!string.IsNullOrWhiteSpace(status) &&
                status != "All")
            {
                if (status == "Active")
                {
                    query = query.Where(c => c.IsActive);
                }
                else if (status == "Inactive")
                {
                    query = query.Where(c => !c.IsActive);
                }
                else if (status == "Expired")
                {
                    query = query.Where(c =>
                        c.ExpiryDate.HasValue &&
                        c.ExpiryDate.Value < DateTime.UtcNow);
                }
            }

            var coupons = await query
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.DiscountType = discountType;
            ViewBag.Status = status;

            ViewBag.TotalCoupons = await _context.Coupons.CountAsync();

            ViewBag.ActiveCoupons = await _context.Coupons
                .CountAsync(c => c.IsActive);

            ViewBag.InactiveCoupons = await _context.Coupons
                .CountAsync(c => !c.IsActive);

            ViewBag.ExpiredCoupons = await _context.Coupons
                .CountAsync(c =>
                    c.ExpiryDate.HasValue &&
                    c.ExpiryDate.Value < DateTime.UtcNow);

            return View(coupons);
        }

        // GET: Admin/Coupons/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var coupon = await _context.Coupons
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (coupon == null)
            {
                return NotFound();
            }

            return View(coupon);
        }

        // GET: Admin/Coupons/Create
        public IActionResult Create()
        {
            return View(new Coupon
            {
                IsActive = true,
                DiscountType = "Percentage",
                MinimumOrderAmount = 0,
                UsedCount = 0
            });
        }

        // POST: Admin/Coupons/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Coupon coupon)
        {
            coupon.Code = coupon.Code?.Trim().ToUpperInvariant() ?? string.Empty;

            ModelState.Remove(nameof(Coupon.Code));

            if (string.IsNullOrWhiteSpace(coupon.Code))
            {
                ModelState.AddModelError(
                    nameof(Coupon.Code),
                    "Coupon code is required.");
            }

            if (coupon.DiscountType != "Percentage" &&
                coupon.DiscountType != "Fixed")
            {
                ModelState.AddModelError(
                    nameof(Coupon.DiscountType),
                    "Invalid discount type.");
            }

            if (coupon.DiscountType == "Percentage" &&
                coupon.DiscountValue > 100)
            {
                ModelState.AddModelError(
                    nameof(Coupon.DiscountValue),
                    "Percentage discount cannot exceed 100%.");
            }

            if (coupon.StartDate.HasValue &&
                coupon.ExpiryDate.HasValue &&
                coupon.ExpiryDate.Value <= coupon.StartDate.Value)
            {
                ModelState.AddModelError(
                    nameof(Coupon.ExpiryDate),
                    "Expiry date must be after the start date.");
            }

            if (coupon.UsageLimit.HasValue &&
                coupon.UsageLimit.Value < 1)
            {
                ModelState.AddModelError(
                    nameof(Coupon.UsageLimit),
                    "Usage limit must be at least 1.");
            }

            var codeExists = await _context.Coupons
                .AnyAsync(c => c.Code == coupon.Code);

            if (codeExists)
            {
                ModelState.AddModelError(
                    nameof(Coupon.Code),
                    "This coupon code already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View(coupon);
            }

            coupon.CreatedAt = DateTime.UtcNow;
            coupon.UsedCount = 0;

            _context.Coupons.Add(coupon);
            await _context.SaveChangesAsync();

            TempData["CouponSuccess"] =
                "Coupon created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Coupons/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Id == id);

            if (coupon == null)
            {
                return NotFound();
            }

            return View(coupon);
        }

        // POST: Admin/Coupons/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Coupon coupon)
        {
            if (id != coupon.Id)
            {
                return NotFound();
            }

            coupon.Code = coupon.Code?.Trim().ToUpperInvariant()
                          ?? string.Empty;

            ModelState.Remove(nameof(Coupon.Code));

            if (string.IsNullOrWhiteSpace(coupon.Code))
            {
                ModelState.AddModelError(
                    nameof(Coupon.Code),
                    "Coupon code is required.");
            }

            if (coupon.DiscountType != "Percentage" &&
                coupon.DiscountType != "Fixed")
            {
                ModelState.AddModelError(
                    nameof(Coupon.DiscountType),
                    "Invalid discount type.");
            }

            if (coupon.DiscountType == "Percentage" &&
                coupon.DiscountValue > 100)
            {
                ModelState.AddModelError(
                    nameof(Coupon.DiscountValue),
                    "Percentage discount cannot exceed 100%.");
            }

            if (coupon.StartDate.HasValue &&
                coupon.ExpiryDate.HasValue &&
                coupon.ExpiryDate.Value <= coupon.StartDate.Value)
            {
                ModelState.AddModelError(
                    nameof(Coupon.ExpiryDate),
                    "Expiry date must be after the start date.");
            }

            if (coupon.UsageLimit.HasValue &&
                coupon.UsageLimit.Value < 1)
            {
                ModelState.AddModelError(
                    nameof(Coupon.UsageLimit),
                    "Usage limit must be at least 1.");
            }

            var codeExists = await _context.Coupons
                .AnyAsync(c =>
                    c.Code == coupon.Code &&
                    c.Id != coupon.Id);

            if (codeExists)
            {
                ModelState.AddModelError(
                    nameof(Coupon.Code),
                    "This coupon code already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View(coupon);
            }

            var existingCoupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Id == id);

            if (existingCoupon == null)
            {
                return NotFound();
            }

            existingCoupon.Code = coupon.Code;
            existingCoupon.Description = coupon.Description;
            existingCoupon.DiscountType = coupon.DiscountType;
            existingCoupon.DiscountValue = coupon.DiscountValue;
            existingCoupon.MinimumOrderAmount =
                coupon.MinimumOrderAmount;
            existingCoupon.StartDate = coupon.StartDate;
            existingCoupon.ExpiryDate = coupon.ExpiryDate;
            existingCoupon.UsageLimit = coupon.UsageLimit;
            existingCoupon.IsActive = coupon.IsActive;

            await _context.SaveChangesAsync();

            TempData["CouponSuccess"] =
                "Coupon updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Coupons/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Id == id);

            if (coupon == null)
            {
                return NotFound();
            }

            coupon.IsActive = !coupon.IsActive;

            await _context.SaveChangesAsync();

            TempData["CouponSuccess"] =
                coupon.IsActive
                    ? "Coupon activated successfully."
                    : "Coupon deactivated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Coupons/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Id == id);

            if (coupon == null)
            {
                return NotFound();
            }

            _context.Coupons.Remove(coupon);
            await _context.SaveChangesAsync();

            TempData["CouponSuccess"] =
                "Coupon deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}