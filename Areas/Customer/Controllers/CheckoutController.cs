using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;
using ShopSphere.Models;
using ShopSphere.Services;

namespace ShopSphere.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize(Roles = "Customer")]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISSLCOMMERZService _sslCommerzService;

        public CheckoutController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ISSLCOMMERZService sslCommerzService)
        {
            _context = context;
            _userManager = userManager;
            _sslCommerzService = sslCommerzService;
        }


        // =========================================================
        // GET: /Customer/Checkout
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }


            // ---------------------------------------------------------
            // Get cart
            // ---------------------------------------------------------

            var cart = await GetCartAsync(userId);

            if (cart == null)
            {
                TempData["ErrorMessage"] = "Your cart is empty.";

                return RedirectToAction(
                    "Index",
                    "Cart",
                    new { area = "Customer" });
            }


            var cartItems = await _context.CartItems
                .Include(ci => ci.Product)
                .Where(ci => ci.CartId == cart.Id)
                .OrderBy(ci => ci.Id)
                .ToListAsync();

            if (!cartItems.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty.";

                return RedirectToAction(
                    "Index",
                    "Cart",
                    new { area = "Customer" });
            }


            // ---------------------------------------------------------
            // Validate products and stock
            // ---------------------------------------------------------

            foreach (var item in cartItems)
            {
                if (item.Product == null ||
                    !item.Product.IsActive ||
                    item.Product.StockQuantity < item.Quantity)
                {
                    TempData["ErrorMessage"] =
                        "One or more products in your cart are no longer available in the requested quantity.";

                    return RedirectToAction(
                        "Index",
                        "Cart",
                        new { area = "Customer" });
                }
            }


            // ---------------------------------------------------------
            // Get current customer
            // ---------------------------------------------------------

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return Challenge();
            }


            // ---------------------------------------------------------
            // Get customer's saved addresses
            // ---------------------------------------------------------

            var addresses = await _context.CustomerAddresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.Addresses = addresses;


            // ---------------------------------------------------------
            // Calculate totals
            // ---------------------------------------------------------

            var subtotal = cartItems.Sum(item =>
    (item.Product?.Price ?? 0m) * item.Quantity);

            decimal shippingFee =
                subtotal > 5000m ? 0m : 100m;


            // =========================================================
            // APPLIED COUPON
            // =========================================================

            decimal discountAmount = 0m;

            string appliedCouponCode =
                TempData["AppliedCouponCode"]?.ToString() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(appliedCouponCode))
            {
                var coupon = await _context.Coupons
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c =>
                        c.Code == appliedCouponCode);

                if (coupon != null)
                {
                    var now = DateTime.UtcNow;

                    var isValid =
                        coupon.IsActive &&
                        (!coupon.StartDate.HasValue ||
                         now >= coupon.StartDate.Value) &&
                        (!coupon.ExpiryDate.HasValue ||
                         now <= coupon.ExpiryDate.Value) &&
                        (!coupon.UsageLimit.HasValue ||
                         coupon.UsedCount < coupon.UsageLimit.Value) &&
                        subtotal >= coupon.MinimumOrderAmount;

                    if (isValid)
                    {
                        if (coupon.DiscountType == "Percentage")
                        {
                            discountAmount =
                                subtotal * coupon.DiscountValue / 100m;
                        }
                        else
                        {
                            discountAmount =
                                coupon.DiscountValue;
                        }

                        if (discountAmount > subtotal)
                        {
                            discountAmount = subtotal;
                        }
                    }
                    else
                    {
                        appliedCouponCode = string.Empty;
                        discountAmount = 0m;

                        TempData["CouponError"] =
                            "The applied coupon is no longer valid.";
                    }
                }
                else
                {
                    appliedCouponCode = string.Empty;
                    discountAmount = 0m;
                }
            }


            // =========================================================
            // FINAL TOTAL
            // =========================================================

            var total =
                subtotal + shippingFee - discountAmount;

            // ---------------------------------------------------------
            // Send checkout data to View
            // ---------------------------------------------------------

            ViewBag.CartItems = cartItems;

            ViewBag.Subtotal = subtotal;

            ViewBag.ShippingFee = shippingFee;
            ViewBag.DiscountAmount = discountAmount;

            ViewBag.AppliedCouponCode = appliedCouponCode;
            ViewBag.Total = total;

            ViewBag.CustomerName =
                user.FullName ?? string.Empty;

            ViewBag.CustomerEmail =
                user.Email ?? string.Empty;


            return View();
        }



        // =========================================================
        // POST: /Customer/Checkout/ApplyCoupon
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyCoupon()
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }


            // ---------------------------------------------------------
            // Get coupon code directly from submitted form
            // ---------------------------------------------------------

            var couponCode =
                Request.Form["couponCode"].ToString();


            if (string.IsNullOrWhiteSpace(couponCode))
            {
                TempData["CouponError"] =
                    "Please enter a coupon code.";

                return RedirectToAction(nameof(Index));
            }


            var code =
                couponCode.Trim().ToUpperInvariant();


            // ---------------------------------------------------------
            // Find coupon
            // ---------------------------------------------------------

            var coupon = await _context.Coupons
                .AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    c.Code == code);


            if (coupon == null)
            {
                TempData["CouponError"] =
                    $"Coupon '{code}' was not found.";

                return RedirectToAction(nameof(Index));
            }


            // ---------------------------------------------------------
            // Active check
            // ---------------------------------------------------------

            if (!coupon.IsActive)
            {
                TempData["CouponError"] =
                    "This coupon is currently inactive.";

                return RedirectToAction(nameof(Index));
            }


            // ---------------------------------------------------------
            // Date validation
            // ---------------------------------------------------------

            var now = DateTime.UtcNow;


            if (coupon.StartDate.HasValue &&
                now < coupon.StartDate.Value)
            {
                TempData["CouponError"] =
                    "This coupon is not active yet.";

                return RedirectToAction(nameof(Index));
            }


            if (coupon.ExpiryDate.HasValue &&
                now > coupon.ExpiryDate.Value)
            {
                TempData["CouponError"] =
                    "This coupon has expired.";

                return RedirectToAction(nameof(Index));
            }


            // ---------------------------------------------------------
            // Usage limit
            // ---------------------------------------------------------

            if (coupon.UsageLimit.HasValue &&
                coupon.UsedCount >= coupon.UsageLimit.Value)
            {
                TempData["CouponError"] =
                    "This coupon has reached its usage limit.";

                return RedirectToAction(nameof(Index));
            }


            // ---------------------------------------------------------
            // Get cart
            // ---------------------------------------------------------

            var cart = await GetCartAsync(userId);


            if (cart == null)
            {
                TempData["CouponError"] =
                    "Your cart is empty.";

                return RedirectToAction(nameof(Index));
            }


            var cartItems = await _context.CartItems
                .Include(ci => ci.Product)
                .Where(ci => ci.CartId == cart.Id)
                .ToListAsync();


            if (!cartItems.Any())
            {
                TempData["CouponError"] =
                    "Your cart is empty.";

                return RedirectToAction(nameof(Index));
            }


            // ---------------------------------------------------------
            // Calculate subtotal
            // ---------------------------------------------------------

            var subtotal = cartItems.Sum(item =>
                (item.Product?.Price ?? 0m) * item.Quantity);


            // ---------------------------------------------------------
            // Minimum order validation
            // ---------------------------------------------------------

            if (subtotal < coupon.MinimumOrderAmount)
            {
                TempData["CouponError"] =
                    $"Minimum order amount for this coupon is ৳{coupon.MinimumOrderAmount:N2}.";

                return RedirectToAction(nameof(Index));
            }


            // ---------------------------------------------------------
            // Calculate discount
            // ---------------------------------------------------------

            decimal discountAmount;


            if (coupon.DiscountType == "Percentage")
            {
                discountAmount =
                    subtotal * coupon.DiscountValue / 100m;
            }
            else
            {
                discountAmount =
                    coupon.DiscountValue;
            }


            // ---------------------------------------------------------
            // Prevent discount from exceeding subtotal
            // ---------------------------------------------------------

            if (discountAmount > subtotal)
            {
                discountAmount = subtotal;
            }


            // ---------------------------------------------------------
            // Save coupon information for checkout
            // ---------------------------------------------------------

            TempData["CouponSuccess"] =
                $"Coupon {coupon.Code} applied successfully. Discount: ৳{discountAmount:N2}";

            TempData["AppliedCouponCode"] =
                coupon.Code;

            TempData["CouponDiscount"] =
                discountAmount.ToString("F2");


            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // POST: /Customer/Checkout/PlaceOrder
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(
            int addressId,
            string? couponCode)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }


            // ---------------------------------------------------------
            // Validate selected saved address
            // ---------------------------------------------------------

            var selectedAddress =
                await _context.CustomerAddresses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a =>
                        a.Id == addressId &&
                        a.UserId == userId);


            if (selectedAddress == null)
            {
                TempData["ErrorMessage"] =
                    "Please select a valid delivery address.";

                return RedirectToAction(nameof(Index));
            }


            // ---------------------------------------------------------
            // Get cart
            // ---------------------------------------------------------

            var cart = await GetCartAsync(userId);

            if (cart == null)
            {
                TempData["ErrorMessage"] =
                    "Your cart is empty.";

                return RedirectToAction(
                    "Index",
                    "Cart",
                    new { area = "Customer" });
            }


            var cartItems = await _context.CartItems
                .Include(ci => ci.Product)
                .Where(ci => ci.CartId == cart.Id)
                .OrderBy(ci => ci.Id)
                .ToListAsync();


            if (!cartItems.Any())
            {
                TempData["ErrorMessage"] =
                    "Your cart is empty.";

                return RedirectToAction(
                    "Index",
                    "Cart",
                    new { area = "Customer" });
            }


            // ---------------------------------------------------------
            // Re-check product and stock
            // ---------------------------------------------------------

            foreach (var item in cartItems)
            {
                if (item.Product == null)
                {
                    TempData["ErrorMessage"] =
                        "A product in your cart is no longer available.";

                    return RedirectToAction(nameof(Index));
                }


                if (!item.Product.IsActive)
                {
                    TempData["ErrorMessage"] =
                        $"{item.Product.Name} is no longer available.";

                    return RedirectToAction(nameof(Index));
                }


                if (item.Product.StockQuantity < item.Quantity)
                {
                    TempData["ErrorMessage"] =
                        $"Not enough stock available for {item.Product.Name}.";

                    return RedirectToAction(nameof(Index));
                }
                if (item.Product.VendorId.HasValue)
                {
                    var isOwnProduct = await _context.Vendors
                        .AnyAsync(v =>
                            v.Id == item.Product.VendorId.Value &&
                            v.UserId == userId);

                    if (isOwnProduct)
                    {
                        TempData["ErrorMessage"] =
                            $"You cannot purchase your own product: {item.Product.Name}.";

                        return RedirectToAction(nameof(Index));
                    }
                }
            }


            // ---------------------------------------------------------
            // Calculate totals from database
            // ---------------------------------------------------------

            var subtotal = cartItems.Sum(item =>
                (item.Product?.Price ?? 0m) * item.Quantity);


            decimal shippingFee =
                subtotal > 5000m ? 0m : 100m;


            // =========================================================
            // VALIDATE COUPON
            // =========================================================

            decimal discountAmount = 0m;

            string? appliedCouponCode = null;


            if (!string.IsNullOrWhiteSpace(couponCode))
            {
                var normalizedCouponCode =
                    couponCode.Trim().ToUpperInvariant();


                var coupon = await _context.Coupons
                    .FirstOrDefaultAsync(c =>
                        c.Code == normalizedCouponCode);


                if (coupon == null)
                {
                    TempData["CouponError"] =
                        "Invalid coupon code.";

                    return RedirectToAction(nameof(Index));
                }


                if (!coupon.IsActive)
                {
                    TempData["CouponError"] =
                        "This coupon is currently inactive.";

                    return RedirectToAction(nameof(Index));
                }


                var now = DateTime.UtcNow;


                if (coupon.StartDate.HasValue &&
                    now < coupon.StartDate.Value)
                {
                    TempData["CouponError"] =
                        "This coupon is not active yet.";

                    return RedirectToAction(nameof(Index));
                }


                if (coupon.ExpiryDate.HasValue &&
                    now > coupon.ExpiryDate.Value)
                {
                    TempData["CouponError"] =
                        "This coupon has expired.";

                    return RedirectToAction(nameof(Index));
                }


                if (coupon.UsageLimit.HasValue &&
                    coupon.UsedCount >= coupon.UsageLimit.Value)
                {
                    TempData["CouponError"] =
                        "This coupon has reached its usage limit.";

                    return RedirectToAction(nameof(Index));
                }


                if (subtotal < coupon.MinimumOrderAmount)
                {
                    TempData["CouponError"] =
                        $"Minimum order amount for this coupon is ৳{coupon.MinimumOrderAmount:N2}.";

                    return RedirectToAction(nameof(Index));
                }


                if (coupon.DiscountType == "Percentage")
                {
                    discountAmount =
                        subtotal * coupon.DiscountValue / 100m;
                }
                else
                {
                    discountAmount =
                        coupon.DiscountValue;
                }


                // Never allow discount to exceed subtotal.
                if (discountAmount > subtotal)
                {
                    discountAmount = subtotal;
                }


                appliedCouponCode =
                    coupon.Code;
            }


            // =========================================================
            // FINAL TOTAL
            // =========================================================

            var total =
                subtotal + shippingFee - discountAmount;


            // =========================================================
            // CREATE ORDER
            // =========================================================

            var order = new Order
            {
                UserId = userId,

                CustomerName =
                    selectedAddress.FullName.Trim(),

                CustomerEmail =
                    (await _userManager.GetUserAsync(User))?.Email
                    ?? string.Empty,

                ShippingAddress =
                    selectedAddress.AddressLine.Trim(),

                City =
                    selectedAddress.City.Trim(),

                PostalCode =
                    selectedAddress.PostalCode?.Trim()
                    ?? string.Empty,

                PhoneNumber =
                    selectedAddress.PhoneNumber.Trim(),

                Subtotal = subtotal,

                ShippingFee = shippingFee,

                DiscountAmount = discountAmount,

                CouponCode = appliedCouponCode,

                TotalAmount = total,

                Status = "Pending",

                PaymentStatus = "Pending",

                CreatedAt = DateTime.UtcNow
            };


            _context.Orders.Add(order);

            await _context.SaveChangesAsync();


            // =========================================================
            // CREATE ORDER ITEMS + RESERVE STOCK
            // =========================================================

            foreach (var cartItem in cartItems)
            {
                var product = cartItem.Product!;


                var orderItem = new OrderItem
                {
                    OrderId = order.Id,

                    ProductId = product.Id,

                    ProductName = product.Name,

                    UnitPrice = product.Price,

                    Quantity = cartItem.Quantity,

                    Subtotal =
                        product.Price * cartItem.Quantity
                };


                _context.OrderItems.Add(orderItem);


                // Reserve stock for this order
                product.StockQuantity -= cartItem.Quantity;
            }


            // =========================================================
            // CREATE PAYMENT
            // =========================================================

            // SSLCOMMERZ tran_id must be <= 30 characters.
            var transactionId =
                $"SS{order.Id}{Guid.NewGuid():N}";


            transactionId =
                transactionId.Length > 30
                    ? transactionId.Substring(0, 30)
                    : transactionId;


            var payment = new Payment
            {
                OrderId = order.Id,

                TranId = transactionId,

                Amount = total,

                Currency = "BDT",

                Status = "Pending",

                GatewayStatus = "INITIATED",

                CreatedAt = DateTime.UtcNow
            };


            _context.Payments.Add(payment);


            // =========================================================
            // CLEAR CART
            // =========================================================

            _context.CartItems.RemoveRange(cartItems);

            await _context.SaveChangesAsync();


            // =========================================================
            // CREATE SSLCOMMERZ SESSION
            // =========================================================

            SSLCOMMERZSessionResult sessionResult;


            try
            {
                sessionResult =
                    await _sslCommerzService
                        .CreatePaymentSessionAsync(
                            order,
                            payment);
            }
            catch (Exception ex)
            {
                sessionResult = new SSLCOMMERZSessionResult
                {
                    Success = false,

                    Message =
                        $"Payment gateway error: {ex.Message}"
                };
            }


            // =========================================================
            // SESSION CREATION FAILED
            // =========================================================

            if (!sessionResult.Success ||
                string.IsNullOrWhiteSpace(
                    sessionResult.GatewayPageUrl))
            {
                // Restore reserved stock
                foreach (var cartItem in cartItems)
                {
                    if (cartItem.Product != null)
                    {
                        cartItem.Product.StockQuantity +=
                            cartItem.Quantity;
                    }
                }


                // Restore cart
                foreach (var cartItem in cartItems)
                {
                    var restoredCartItem = new CartItem
                    {
                        CartId = cartItem.CartId,

                        ProductId = cartItem.ProductId,

                        Quantity = cartItem.Quantity
                    };


                    _context.CartItems.Add(
                        restoredCartItem);
                }


                // Remove payment
                _context.Payments.Remove(payment);


                // Remove order items
                var orderItems =
                    await _context.OrderItems
                        .Where(oi =>
                            oi.OrderId == order.Id)
                        .ToListAsync();


                _context.OrderItems.RemoveRange(
                    orderItems);


                // Remove order
                _context.Orders.Remove(order);


                await _context.SaveChangesAsync();


                TempData["ErrorMessage"] =
                    sessionResult.Message ??
                    "Unable to connect to SSLCOMMERZ payment gateway. Please try again.";


                return RedirectToAction(nameof(Index));
            }


            // =========================================================
            // SAVE GATEWAY SESSION DATA
            // =========================================================

            payment.SessionKey =
                sessionResult.SessionKey;


            if (!string.IsNullOrWhiteSpace(
                    sessionResult.TransactionId))
            {
                payment.TranId =
                    sessionResult.TransactionId;
            }


            payment.GatewayStatus =
                "SESSION_CREATED";


            payment.GatewayResponse =
                sessionResult.Message;


            await _context.SaveChangesAsync();


            // =========================================================
            // REDIRECT TO SSLCOMMERZ
            // =========================================================

            return Redirect(
                sessionResult.GatewayPageUrl);
        }


        // =========================================================
        // GET: /Customer/Checkout/Success/5
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Success(int id)
        {
            var userId =
                _userManager.GetUserId(User);


            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }


            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o =>
                    o.Id == id &&
                    o.UserId == userId);


            if (order == null)
            {
                return NotFound();
            }


            return View(order);
        }


        // =========================================================
        // HELPER
        // =========================================================

        private async Task<Cart?> GetCartAsync(
            string userId)
        {
            return await _context.Carts
                .FirstOrDefaultAsync(c =>
                    c.UserId == userId);
        }
    }
}