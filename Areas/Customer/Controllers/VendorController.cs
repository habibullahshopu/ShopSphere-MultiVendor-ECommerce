using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;
using ShopSphere.Models;
using ShopSphere.ViewModels;
using VendorModel = ShopSphere.Models.Vendor;

namespace ShopSphere.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize(Roles = "Customer")]
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
        // GET: /Customer/Vendor/Register
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Register()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var existingVendor = await _context.Vendors
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.UserId == user.Id);

            // No application yet
            if (existingVendor == null)
            {
                return View();
            }

            // Already approved
            if (existingVendor.IsApproved && existingVendor.IsActive)
            {
                TempData["Success"] =
                    "You are already an approved vendor.";

                return RedirectToAction(
                    "Index",
                    "Dashboard",
                    new { area = "Vendor" });
            }

            // Application exists but is not approved yet
            return RedirectToAction(
                nameof(ApplicationSubmitted));
        }


        // =========================================================
        // POST: /Customer/Vendor/Register
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            VendorRegistrationViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            // Validate form
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Prevent duplicate vendor applications
            var existingVendor = await _context.Vendors
                .FirstOrDefaultAsync(v => v.UserId == user.Id);

            if (existingVendor != null)
            {
                return RedirectToAction(
                    nameof(ApplicationSubmitted));
            }

            // Create vendor application
            var vendor = new VendorModel
            {
                UserId = user.Id,

                StoreName = model.StoreName,
                StoreDescription = model.StoreDescription,
                StoreAddress = model.StoreAddress,
                City = model.City,
                Country = model.Country,

                IsApproved = false,
                IsActive = true,

                CommissionRate = 10.00m,

                CreatedAt = DateTime.UtcNow
            };

            _context.Vendors.Add(vendor);

            // Mark customer as vendor applicant
            user.IsVendor = true;
            user.IsVendorApproved = false;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Your vendor application has been submitted successfully.";

            // Show application submitted page
            return RedirectToAction(
                nameof(ApplicationSubmitted));
        }


        // =========================================================
        // GET: /Customer/Vendor/ApplicationSubmitted
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> ApplicationSubmitted()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var vendor = await _context.Vendors
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.UserId == user.Id);

            // No application found
            if (vendor == null)
            {
                return RedirectToAction(
                    nameof(Register));
            }

            // Admin already approved the vendor
            if (vendor.IsApproved && vendor.IsActive)
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard",
                    new { area = "Vendor" });
            }

            // Application is still pending
            return View(vendor);
        }
    }
}