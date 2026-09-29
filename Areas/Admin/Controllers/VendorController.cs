using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;
using ShopSphere.Models;
using ShopSphere.ViewModels;

namespace ShopSphere.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class VendorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public VendorController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =========================================================
        // VENDOR LIST
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var vendors = await _context.Vendors
                .Include(v => v.User)
                .OrderByDescending(v => v.CreatedAt)
                .Select(v => new AdminVendorViewModel
                {
                    Id = v.Id,
                    UserId = v.UserId,

                    StoreName = v.StoreName,
                    StoreDescription = v.StoreDescription,
                    StoreAddress = v.StoreAddress,
                    City = v.City,
                    Country = v.Country,

                    IsApproved = v.IsApproved,
                    IsActive = v.IsActive,

                    CommissionRate = v.CommissionRate,

                    CreatedAt = v.CreatedAt,
                    ApprovedAt = v.ApprovedAt,

                    FullName = v.User != null
                        ? v.User.FullName
                        : null,

                    Email = v.User != null
                        ? v.User.Email
                        : null,

                    PhoneNumber = v.User != null
                        ? v.User.PhoneNumber
                        : null
                })
                .ToListAsync();

            return View(vendors);
        }


        // =========================================================
        // VENDOR DETAILS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var vendor = await _context.Vendors
                .Include(v => v.User)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (vendor == null)
            {
                return NotFound();
            }

            var model = new AdminVendorViewModel
            {
                Id = vendor.Id,
                UserId = vendor.UserId,

                StoreName = vendor.StoreName,
                StoreDescription = vendor.StoreDescription,
                StoreAddress = vendor.StoreAddress,
                City = vendor.City,
                Country = vendor.Country,

                IsApproved = vendor.IsApproved,
                IsActive = vendor.IsActive,

                CommissionRate = vendor.CommissionRate,

                CreatedAt = vendor.CreatedAt,
                ApprovedAt = vendor.ApprovedAt,

                FullName = vendor.User?.FullName,
                Email = vendor.User?.Email,
                PhoneNumber = vendor.User?.PhoneNumber
            };

            return View(model);
        }


        // =========================================================
        // APPROVE VENDOR
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var vendor = await _context.Vendors
                .Include(v => v.User)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (vendor == null)
            {
                return NotFound();
            }

            if (vendor.User == null)
            {
                TempData["Error"] =
                    "Vendor user account could not be found.";

                return RedirectToAction(
                    nameof(Index),
                    "Vendor",
                    new { area = "Admin" });
            }

            // ---------------------------------------------
            // APPROVE VENDOR
            // ---------------------------------------------

            vendor.IsApproved = true;
            vendor.IsActive = true;
            vendor.ApprovedAt = DateTime.UtcNow;

            // ---------------------------------------------
            // UPDATE USER VENDOR STATUS
            // ---------------------------------------------

            vendor.User.IsVendor = true;
            vendor.User.IsVendorApproved = true;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Vendor '{vendor.StoreName}' has been approved successfully.";

            // ---------------------------------------------
            // IMPORTANT:
            // Explicitly return to ADMIN Vendor Management
            // ---------------------------------------------

            return RedirectToAction(
                nameof(Index),
                "Vendor",
                new { area = "Admin" });
        }


        // =========================================================
        // REJECT VENDOR
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var vendor = await _context.Vendors
                .Include(v => v.User)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (vendor == null)
            {
                return NotFound();
            }

            if (vendor.User == null)
            {
                TempData["Error"] =
                    "Vendor user account could not be found.";

                return RedirectToAction(
                    nameof(Index),
                    "Vendor",
                    new { area = "Admin" });
            }

            // ---------------------------------------------
            // REJECT VENDOR
            // ---------------------------------------------

            vendor.IsApproved = false;
            vendor.IsActive = false;
            vendor.ApprovedAt = null;

            // ---------------------------------------------
            // UPDATE USER VENDOR STATUS
            // ---------------------------------------------

            vendor.User.IsVendor = true;
            vendor.User.IsVendorApproved = false;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Vendor '{vendor.StoreName}' has been rejected.";

            // ---------------------------------------------
            // IMPORTANT:
            // Explicitly return to ADMIN Vendor Management
            // ---------------------------------------------

            return RedirectToAction(
                nameof(Index),
                "Vendor",
                new { area = "Admin" });
        }
    }
}