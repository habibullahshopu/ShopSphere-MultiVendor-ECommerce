using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ShopSphere.Models;
using ShopSphere.ViewModels;

namespace ShopSphere.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CustomerProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public CustomerProfileController(
            UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }


        // ============================================
        // PROFILE - GET
        // ============================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }


            var model = new CustomerProfileViewModel
            {
                FullName = user.FullName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                City = user.City,
                PostalCode = user.PostalCode
            };


            ViewBag.CreatedAt = user.CreatedAt;
            ViewBag.IsVendor = user.IsVendor;
            ViewBag.IsVendorApproved = user.IsVendorApproved;


            return View(model);
        }


        // ============================================
        // PROFILE - POST
        // ============================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(
            CustomerProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }


            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }


            // ========================================
            // Update profile information
            // ========================================

            user.FullName = model.FullName.Trim();

            user.PhoneNumber =
                string.IsNullOrWhiteSpace(model.PhoneNumber)
                    ? null
                    : model.PhoneNumber.Trim();

            user.Address =
                string.IsNullOrWhiteSpace(model.Address)
                    ? null
                    : model.Address.Trim();

            user.City =
                string.IsNullOrWhiteSpace(model.City)
                    ? null
                    : model.City.Trim();

            user.PostalCode =
                string.IsNullOrWhiteSpace(model.PostalCode)
                    ? null
                    : model.PostalCode.Trim();


            // ========================================
            // Save changes
            // ========================================

            var result =
                await _userManager.UpdateAsync(user);


            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        error.Description);
                }

                return View(model);
            }


            TempData["SuccessMessage"] =
                "Your profile has been updated successfully.";


            return RedirectToAction(nameof(Index));
        }
    }
}