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
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        // =========================================================
        // GET: /Customer/Cart
        // =========================================================
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var cart = await GetOrCreateCartAsync(userId);

            var cartItems = await _context.CartItems
                .Include(ci => ci.Product)
                .ThenInclude(p => p!.Category)
                .Where(ci => ci.CartId == cart.Id)
                .OrderBy(ci => ci.Id)
                .ToListAsync();

            ViewBag.CartTotal = cartItems.Sum(ci =>
                (ci.Product?.Price ?? 0) * ci.Quantity);

            ViewBag.TotalItems = cartItems.Sum(ci => ci.Quantity);

            return View(cartItems);
        }


        // =========================================================
        // POST: /Customer/Cart/AddToCart
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(
            int productId,
            int quantity = 1)
        {
            if (quantity < 1)
            {
                quantity = 1;
            }

            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            // Find active product
            var product = await _context.Products
                .FirstOrDefaultAsync(p =>
                    p.Id == productId &&
                    p.IsActive);

            if (product == null)
            {
                TempData["ErrorMessage"] =
                    "Product not found.";

                return RedirectToAction(
                    "Index",
                    "Products");
            }
            // Vendor cannot purchase their own product
            if (product.VendorId.HasValue)
            {
                var isOwnProduct = await _context.Vendors
                    .AnyAsync(v =>
                        v.Id == product.VendorId.Value &&
                        v.UserId == userId);

                if (isOwnProduct)
                {
                    TempData["ErrorMessage"] =
                        "You cannot purchase your own product.";

                    return RedirectToAction(
                        "Details",
                        "Products",
                        new { id = productId });
                }
            }

            // Check stock
            if (product.StockQuantity <= 0)
            {
                TempData["ErrorMessage"] =
                    "This product is currently out of stock.";

                return RedirectToAction(
                    "Details",
                    "Products",
                    new { id = productId });
            }

            // Cannot add more than available stock
            if (quantity > product.StockQuantity)
            {
                quantity = product.StockQuantity;
            }

            var cart = await GetOrCreateCartAsync(userId);

            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(ci =>
                    ci.CartId == cart.Id &&
                    ci.ProductId == productId);

            if (existingItem == null)
            {
                existingItem = new CartItem
                {
                    CartId = cart.Id,
                    ProductId = productId,
                    Quantity = quantity
                };

                _context.CartItems.Add(existingItem);
            }
            else
            {
                var newQuantity =
                    existingItem.Quantity + quantity;

                existingItem.Quantity =
                    Math.Min(
                        newQuantity,
                        product.StockQuantity);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"{product.Name} added to your cart.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // POST: /Customer/Cart/UpdateQuantity
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(
            int cartItemId,
            int quantity)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            if (quantity < 1)
            {
                quantity = 1;
            }

            var cart = await GetOrCreateCartAsync(userId);

            var cartItem = await _context.CartItems
                .Include(ci => ci.Product)
                .FirstOrDefaultAsync(ci =>
                    ci.Id == cartItemId &&
                    ci.CartId == cart.Id);

            if (cartItem == null)
            {
                TempData["ErrorMessage"] =
                    "Cart item not found.";

                return RedirectToAction(nameof(Index));
            }
            // Vendor cannot purchase their own product
            if (cartItem.Product?.VendorId.HasValue == true)
            {
                var isOwnProduct = await _context.Vendors
                    .AnyAsync(v =>
                        v.Id == cartItem.Product.VendorId.Value &&
                        v.UserId == userId);

                if (isOwnProduct)
                {
                    _context.CartItems.Remove(cartItem);

                    await _context.SaveChangesAsync();

                    TempData["ErrorMessage"] =
                        "You cannot purchase your own product.";

                    return RedirectToAction(nameof(Index));
                }
            }

            if (cartItem.Product == null ||
                !cartItem.Product.IsActive)
            {
                _context.CartItems.Remove(cartItem);

                await _context.SaveChangesAsync();

                TempData["ErrorMessage"] =
                    "This product is no longer available.";

                return RedirectToAction(nameof(Index));
            }

            if (cartItem.Product.StockQuantity <= 0)
            {
                _context.CartItems.Remove(cartItem);

                await _context.SaveChangesAsync();

                TempData["ErrorMessage"] =
                    "This product is out of stock.";

                return RedirectToAction(nameof(Index));
            }

            cartItem.Quantity =
                Math.Min(
                    quantity,
                    cartItem.Product.StockQuantity);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Cart quantity updated.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // POST: /Customer/Cart/Remove
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(
            int cartItemId)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var cart = await GetOrCreateCartAsync(userId);

            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(ci =>
                    ci.Id == cartItemId &&
                    ci.CartId == cart.Id);

            if (cartItem != null)
            {
                _context.CartItems.Remove(cartItem);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Item removed from cart.";
            }

            return RedirectToAction(nameof(Index));
        }



        // =========================================================
        // POST: /Customer/Cart/Clear
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clear()
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var cart = await GetOrCreateCartAsync(userId);

            var cartItems = await _context.CartItems
                .Where(ci => ci.CartId == cart.Id)
                .ToListAsync();

            if (cartItems.Any())
            {
                _context.CartItems.RemoveRange(cartItems);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Your cart has been cleared.";
            }

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // GET OR CREATE CART
        // =========================================================
        private async Task<Cart> GetOrCreateCartAsync(
            string userId)
        {
            var cart = await _context.Carts
                .FirstOrDefaultAsync(c =>
                    c.UserId == userId);

            if (cart != null)
            {
                return cart;
            }

            cart = new Cart
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Carts.Add(cart);

            await _context.SaveChangesAsync();

            return cart;
        }
    }
}