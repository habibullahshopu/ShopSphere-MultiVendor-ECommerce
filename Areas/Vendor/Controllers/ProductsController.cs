using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;
using ShopSphere.Models;
using ShopSphere.ViewModels;

// Vendor namespace conflict fix
using VendorModel = ShopSphere.Models.Vendor;

namespace ShopSphere.Areas.Vendor.Controllers
{
    [Area("Vendor")]
    [Authorize(Roles = "Customer")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProductsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        // =====================================================
        // Vendor Product List
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var vendor = await GetCurrentVendorAsync();

            if (vendor == null)
            {
                return RedirectToVendorDashboard();
            }

            var products = await _context.Products
                .Where(p => p.VendorId == vendor.Id)
                .Include(p => p.Category)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(products);
        }


        // =====================================================
        // Create Product - GET
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var vendor = await GetCurrentVendorAsync();

            if (vendor == null)
            {
                return RedirectToVendorDashboard();
            }

            await LoadCategoriesAsync();

            return View(new VendorProductViewModel
            {
                IsActive = true
            });
        }


        // =====================================================
        // Create Product - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            VendorProductViewModel model)
        {
            var vendor = await GetCurrentVendorAsync();

            if (vendor == null)
            {
                return RedirectToVendorDashboard();
            }

            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync();
                return View(model);
            }

            var categoryExists = await _context.Categories
                .AnyAsync(c => c.Id == model.CategoryId);

            if (!categoryExists)
            {
                ModelState.AddModelError(
                    nameof(model.CategoryId),
                    "Selected category does not exist.");

                await LoadCategoriesAsync();
                return View(model);
            }

            var product = new Product
            {
                Name = model.Name,
                Description = model.Description,
                Price = model.Price,
                StockQuantity = model.StockQuantity,
                ImageUrl = model.ImageUrl,
                IsActive = model.IsActive,
                CategoryId = model.CategoryId,
                VendorId = vendor.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.Products.Add(product);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Product added successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =====================================================
        // Edit Product - GET
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var vendor = await GetCurrentVendorAsync();

            if (vendor == null)
            {
                return RedirectToVendorDashboard();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(p =>
                    p.Id == id &&
                    p.VendorId == vendor.Id);

            if (product == null)
            {
                return NotFound();
            }

            await LoadCategoriesAsync();

            var model = new VendorProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                ImageUrl = product.ImageUrl,
                IsActive = product.IsActive,
                CategoryId = product.CategoryId
            };

            return View(model);
        }


        // =====================================================
        // Edit Product - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            VendorProductViewModel model)
        {
            var vendor = await GetCurrentVendorAsync();

            if (vendor == null)
            {
                return RedirectToVendorDashboard();
            }

            if (id != model.Id)
            {
                return BadRequest();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(p =>
                    p.Id == id &&
                    p.VendorId == vendor.Id);

            if (product == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync();
                return View(model);
            }

            var categoryExists = await _context.Categories
                .AnyAsync(c => c.Id == model.CategoryId);

            if (!categoryExists)
            {
                ModelState.AddModelError(
                    nameof(model.CategoryId),
                    "Selected category does not exist.");

                await LoadCategoriesAsync();
                return View(model);
            }

            product.Name = model.Name;
            product.Description = model.Description;
            product.Price = model.Price;
            product.StockQuantity = model.StockQuantity;
            product.ImageUrl = model.ImageUrl;
            product.IsActive = model.IsActive;
            product.CategoryId = model.CategoryId;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Product updated successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =====================================================
        // Delete Product - GET
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var vendor = await GetCurrentVendorAsync();

            if (vendor == null)
            {
                return RedirectToVendorDashboard();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p =>
                    p.Id == id &&
                    p.VendorId == vendor.Id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }


        // =====================================================
        // Delete Product - POST
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var vendor = await GetCurrentVendorAsync();

            if (vendor == null)
            {
                return RedirectToVendorDashboard();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(p =>
                    p.Id == id &&
                    p.VendorId == vendor.Id);

            if (product == null)
            {
                return NotFound();
            }

            var hasOrderItems = await _context.OrderItems
                .AnyAsync(oi => oi.ProductId == id);

            if (hasOrderItems)
            {
                TempData["Error"] =
                    "This product cannot be deleted because it has existing orders.";

                return RedirectToAction(nameof(Index));
            }

            var hasCartItems = await _context.CartItems
                .AnyAsync(ci => ci.ProductId == id);

            if (hasCartItems)
            {
                TempData["Error"] =
                    "This product cannot be deleted because it is currently in a cart.";

                return RedirectToAction(nameof(Index));
            }

            _context.Products.Remove(product);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Product deleted successfully.";

            return RedirectToAction(nameof(Index));
        }


        // =====================================================
        // Helpers
        // =====================================================

        private async Task<VendorModel?> GetCurrentVendorAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return null;
            }

            var vendor = await _context.Vendors
                .FirstOrDefaultAsync(v =>
                    v.UserId == user.Id &&
                    v.IsApproved &&
                    v.IsActive);

            return vendor;
        }


        private async Task LoadCategoriesAsync()
        {
            ViewBag.Categories = await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();
        }


        private IActionResult RedirectToVendorDashboard()
        {
            return RedirectToAction(
                "Index",
                "Dashboard",
                new { area = "Vendor" });
        }
    }
}