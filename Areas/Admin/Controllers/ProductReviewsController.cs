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
    public class ProductReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProductReviewsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =========================================================
        // POST: Customer/ProductReviews/Create
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int productId,
            int rating,
            string comment)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            // -----------------------------------------------------
            // Validate product
            // -----------------------------------------------------
            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.Id == productId &&
                    p.IsActive);

            if (product == null)
            {
                TempData["ReviewError"] =
                    "Product not found.";

                return RedirectToAction(
                    "Details",
                    "Products",
                    new
                    {
                        area = "Customer",
                        id = productId
                    });
            }

            // -----------------------------------------------------
            // Validate rating
            // -----------------------------------------------------
            if (rating < 1 || rating > 5)
            {
                TempData["ReviewError"] =
                    "Please select a rating between 1 and 5 stars.";

                return RedirectToAction(
                    "Details",
                    "Products",
                    new
                    {
                        area = "Customer",
                        id = productId
                    });
            }

            // -----------------------------------------------------
            // Validate comment
            // -----------------------------------------------------
            if (string.IsNullOrWhiteSpace(comment))
            {
                TempData["ReviewError"] =
                    "Please write a review comment.";

                return RedirectToAction(
                    "Details",
                    "Products",
                    new
                    {
                        area = "Customer",
                        id = productId
                    });
            }

            comment = comment.Trim();

            if (comment.Length > 2000)
            {
                TempData["ReviewError"] =
                    "Review cannot exceed 2000 characters.";

                return RedirectToAction(
                    "Details",
                    "Products",
                    new
                    {
                        area = "Customer",
                        id = productId
                    });
            }

            // -----------------------------------------------------
            // Check if customer already reviewed this product
            // -----------------------------------------------------
            var existingReview = await _context.ProductReviews
                .FirstOrDefaultAsync(r =>
                    r.UserId == user.Id &&
                    r.ProductId == productId);

            if (existingReview != null)
            {
                TempData["ReviewError"] =
                    "You have already reviewed this product.";

                return RedirectToAction(
                    "Details",
                    "Products",
                    new
                    {
                        area = "Customer",
                        id = productId
                    });
            }

            // -----------------------------------------------------
            // Check whether customer purchased the product
            // and the related payment was successful.
            // -----------------------------------------------------
            var hasPurchased = await _context.Orders
                .Where(o =>
                    o.UserId == user.Id &&
                    o.OrderItems.Any(oi =>
                        oi.ProductId == productId))
                .Join(
                    _context.Payments,
                    order => order.Id,
                    payment => payment.OrderId,
                    (order, payment) => payment
                )
                .AnyAsync(p =>
                    p.Status == "Paid");

            if (!hasPurchased)
            {
                TempData["ReviewError"] =
                    "You can review this product only after purchasing it.";

                return RedirectToAction(
                    "Details",
                    "Products",
                    new
                    {
                        area = "Customer",
                        id = productId
                    });
            }

            // -----------------------------------------------------
            // Create review
            // -----------------------------------------------------
            var review = new ProductReview
            {
                UserId = user.Id,
                ProductId = productId,
                Rating = rating,
                Comment = comment,
                IsApproved = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.ProductReviews.Add(review);

            try
            {
                await _context.SaveChangesAsync();

                TempData["ReviewSuccess"] =
                    "Your review has been submitted successfully.";
            }
            catch (DbUpdateException)
            {
                // Protect against duplicate submission/race condition
                TempData["ReviewError"] =
                    "You have already reviewed this product.";
            }

            return RedirectToAction(
                "Details",
                "Products",
                new
                {
                    area = "Customer",
                    id = productId
                });
        }


        // =========================================================
        // POST: Customer/ProductReviews/Edit
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            int rating,
            string comment)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var review = await _context.ProductReviews
                .FirstOrDefaultAsync(r =>
                    r.Id == id &&
                    r.UserId == user.Id);

            if (review == null)
            {
                TempData["ReviewError"] =
                    "Review not found.";

                return RedirectToAction(
                    "Index",
                    "Products",
                    new
                    {
                        area = "Customer"
                    });
            }

            // Validate rating
            if (rating < 1 || rating > 5)
            {
                TempData["ReviewError"] =
                    "Please select a rating between 1 and 5 stars.";

                return RedirectToAction(
                    "Details",
                    "Products",
                    new
                    {
                        area = "Customer",
                        id = review.ProductId
                    });
            }

            // Validate comment
            if (string.IsNullOrWhiteSpace(comment))
            {
                TempData["ReviewError"] =
                    "Please write a review comment.";

                return RedirectToAction(
                    "Details",
                    "Products",
                    new
                    {
                        area = "Customer",
                        id = review.ProductId
                    });
            }

            comment = comment.Trim();

            if (comment.Length > 2000)
            {
                TempData["ReviewError"] =
                    "Review cannot exceed 2000 characters.";

                return RedirectToAction(
                    "Details",
                    "Products",
                    new
                    {
                        area = "Customer",
                        id = review.ProductId
                    });
            }

            review.Rating = rating;
            review.Comment = comment;
            review.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["ReviewSuccess"] =
                "Your review has been updated successfully.";

            return RedirectToAction(
                "Details",
                "Products",
                new
                {
                    area = "Customer",
                    id = review.ProductId
                });
        }


        // =========================================================
        // POST: Customer/ProductReviews/Delete
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var review = await _context.ProductReviews
                .FirstOrDefaultAsync(r =>
                    r.Id == id &&
                    r.UserId == user.Id);

            if (review == null)
            {
                TempData["ReviewError"] =
                    "Review not found.";

                return RedirectToAction(
                    "Index",
                    "Products",
                    new
                    {
                        area = "Customer"
                    });
            }

            var productId = review.ProductId;

            _context.ProductReviews.Remove(review);

            await _context.SaveChangesAsync();

            TempData["ReviewSuccess"] =
                "Your review has been deleted successfully.";

            return RedirectToAction(
                "Details",
                "Products",
                new
                {
                    area = "Customer",
                    id = productId
                });
        }
    }
}