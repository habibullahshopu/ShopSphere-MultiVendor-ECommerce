using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;

namespace ShopSphere.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReviewsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // REVIEW LIST
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Index(
            string? search,
            int? rating,
            string? status)
        {
            var query = _context.ProductReviews
                .Include(r => r.Product)
                .Include(r => r.User)
                .AsQueryable();

            // Search by product name or customer name
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(r =>
                    (r.Product != null &&
                     r.Product.Name.Contains(search)) ||

                    (r.User != null &&
                     (
                         (r.User.FullName != null &&
                          r.User.FullName.Contains(search)) ||

                         (r.User.Email != null &&
                          r.User.Email.Contains(search))
                     )));
            }

            // Rating filter
            if (rating.HasValue &&
                rating.Value >= 1 &&
                rating.Value <= 5)
            {
                query = query.Where(r =>
                    r.Rating == rating.Value);
            }

            // Approval status filter
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status.Equals(
                    "Approved",
                    StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(r =>
                        r.IsApproved);
                }
                else if (status.Equals(
                    "Pending",
                    StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(r =>
                        !r.IsApproved);
                }
            }

            var reviews = await query
                .OrderByDescending(r => r.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            // Summary counts
            ViewBag.TotalReviews =
                await _context.ProductReviews.CountAsync();

            ViewBag.ApprovedReviews =
                await _context.ProductReviews
                    .CountAsync(r => r.IsApproved);

            ViewBag.PendingReviews =
                await _context.ProductReviews
                    .CountAsync(r => !r.IsApproved);

            ViewBag.AverageRating =
                await _context.ProductReviews
                    .Select(r => (double?)r.Rating)
                    .AverageAsync() ?? 0;

            ViewBag.Search = search;
            ViewBag.Rating = rating;
            ViewBag.Status = status;

            return View(reviews);
        }

        // =========================================================
        // REVIEW DETAILS
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var review = await _context.ProductReviews
                .Include(r => r.Product)
                .Include(r => r.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(r =>
                    r.Id == id);

            if (review == null)
                return NotFound();

            return View(review);
        }

        // =========================================================
        // APPROVE REVIEW
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var review = await _context.ProductReviews
                .FirstOrDefaultAsync(r =>
                    r.Id == id);

            if (review == null)
            {
                TempData["ReviewError"] =
                    "Review not found.";

                return RedirectToAction(nameof(Index));
            }

            review.IsApproved = true;

            await _context.SaveChangesAsync();

            TempData["ReviewSuccess"] =
                "Review approved successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // REJECT / HIDE REVIEW
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var review = await _context.ProductReviews
                .FirstOrDefaultAsync(r =>
                    r.Id == id);

            if (review == null)
            {
                TempData["ReviewError"] =
                    "Review not found.";

                return RedirectToAction(nameof(Index));
            }

            review.IsApproved = false;

            await _context.SaveChangesAsync();

            TempData["ReviewSuccess"] =
                "Review rejected successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // DELETE REVIEW
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var review = await _context.ProductReviews
                .FirstOrDefaultAsync(r =>
                    r.Id == id);

            if (review == null)
            {
                TempData["ReviewError"] =
                    "Review not found.";

                return RedirectToAction(nameof(Index));
            }

            _context.ProductReviews.Remove(review);

            await _context.SaveChangesAsync();

            TempData["ReviewSuccess"] =
                "Review deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}