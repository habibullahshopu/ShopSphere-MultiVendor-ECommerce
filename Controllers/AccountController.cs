using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ShopSphere.Models;
using System.ComponentModel.DataAnnotations;

namespace ShopSphere.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }


        // ============================================
        // REGISTER - GET
        // ============================================

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }


        // ============================================
        // REGISTER - POST
        // ============================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check existing email
            var existingUser =
                await _userManager.FindByEmailAsync(model.Email);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    "Email",
                    "An account with this email already exists.");

                return View(model);
            }


            // ========================================
            // Make sure Customer role exists
            // ========================================

            if (!await _roleManager.RoleExistsAsync("Customer"))
            {
                var roleResult =
                    await _roleManager.CreateAsync(
                        new IdentityRole("Customer"));

                if (!roleResult.Succeeded)
                {
                    ModelState.AddModelError(
                        "",
                        "Unable to create Customer role.");

                    return View(model);
                }
            }


            // ========================================
            // Create ApplicationUser
            // ========================================

            var user = new ApplicationUser
            {
                FullName = model.FullName.Trim(),

                Email = model.Email.Trim(),

                UserName = model.Email.Trim(),

                EmailConfirmed = true,

                IsActive = true,

                CreatedAt = DateTime.UtcNow
            };


            var createResult =
                await _userManager.CreateAsync(
                    user,
                    model.Password);


            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        error.Description);
                }

                return View(model);
            }


            // ========================================
            // Assign Customer role
            // ========================================

            var roleAssignment =
                await _userManager.AddToRoleAsync(
                    user,
                    "Customer");


            if (!roleAssignment.Succeeded)
            {
                await _userManager.DeleteAsync(user);

                ModelState.AddModelError(
                    "",
                    "Account creation failed while assigning Customer role.");

                return View(model);
            }


            // ========================================
            // Automatically login customer
            // ========================================

            await _signInManager.SignInAsync(
                user,
                isPersistent: false);


            TempData["SuccessMessage"] =
                "Account created successfully. Welcome to ShopSphere!";


            // Customer homepage
            return RedirectToAction(
                "Index",
                "Home",
                new { area = "Customer" });
        }


        // ============================================
        // LOGIN - GET
        // ============================================

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            var model = new LoginViewModel
            {
                ReturnUrl = returnUrl
            };

            return View(model);
        }


        // ============================================
        // LOGIN - POST
        // ============================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }


            // Find user
            var user =
                await _userManager.FindByEmailAsync(
                    model.Email);


            if (user == null)
            {
                ModelState.AddModelError(
                    "",
                    "Invalid email or password.");

                return View(model);
            }


            // Check active status
            if (!user.IsActive)
            {
                ModelState.AddModelError(
                    "",
                    "Your account is currently inactive.");

                return View(model);
            }


            // Verify password
            var passwordValid =
                await _userManager.CheckPasswordAsync(
                    user,
                    model.Password);


            if (!passwordValid)
            {
                ModelState.AddModelError(
                    "",
                    "Invalid email or password.");

                return View(model);
            }


            // Login
            await _signInManager.SignInAsync(
                user,
                model.RememberMe);


            // ========================================
            // ADMIN
            // ========================================

            if (await _userManager.IsInRoleAsync(
                    user,
                    "Admin"))
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard",
                    new { area = "Admin" });
            }


            // ========================================
            // VENDOR
            // ========================================

            if (await _userManager.IsInRoleAsync(
                    user,
                    "Vendor"))
            {
                return RedirectToAction(
                    "Index",
                    "Dashboard",
                    new { area = "Vendor" });
            }


            // ========================================
            // CUSTOMER
            // ========================================

            return RedirectToAction(
                "Index",
                "Home",
                new { area = "Customer" });
        }


        // ============================================
        // LOGOUT
        // ============================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction(
                "Index",
                "Home");
        }


        // ============================================
        // ACCESS DENIED
        // ============================================

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }


    // ================================================
    // REGISTER VIEW MODEL
    // ================================================

    public class RegisterViewModel
    {
        [Required]
        [StringLength(
            100,
            MinimumLength = 2)]
        public string FullName { get; set; } = string.Empty;


        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;


        [Required]
        [DataType(
            DataType.Password)]
        [StringLength(
            100,
            MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;


        [Required]
        [DataType(
            DataType.Password)]
        [Compare(
            "Password",
            ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }


    // ================================================
    // LOGIN VIEW MODEL
    // ================================================

    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;


        [Required]
        [DataType(
            DataType.Password)]
        public string Password { get; set; } = string.Empty;


        public bool RememberMe { get; set; }


        public string? ReturnUrl { get; set; }
    }
}