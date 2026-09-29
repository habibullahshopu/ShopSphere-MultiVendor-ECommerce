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
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public WishlistController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var wishlist = await _context.Wishlists
                .Include(w => w.Product)
                    .ThenInclude(p => p!.Category)
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            return View(wishlist);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.Id == productId &&
                    p.IsActive);

            if (product == null)
            {
                return NotFound();
            }

            var exists = await _context.Wishlists
                .AnyAsync(w =>
                    w.UserId == userId &&
                    w.ProductId == productId);

            if (!exists)
            {
                _context.Wishlists.Add(new Wishlist
                {
                    UserId = userId,
                    ProductId = productId,
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Product added to your wishlist.";
            }
            else
            {
                TempData["InfoMessage"] =
                    "This product is already in your wishlist.";
            }

            return RedirectToAction(
                nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var wishlistItem =
                await _context.Wishlists
                    .FirstOrDefaultAsync(w =>
                        w.Id == id &&
                        w.UserId == userId);

            if (wishlistItem == null)
            {
                return NotFound();
            }

            _context.Wishlists.Remove(wishlistItem);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Product removed from your wishlist.";

            return RedirectToAction(
                nameof(Index));
        }
    }
}