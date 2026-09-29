using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;
using ShopSphere.Services;
using System.Text.RegularExpressions;

namespace ShopSphere.Areas.Customer.Controllers
{
    [Area("Customer")]
    [AllowAnonymous]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAISearchService _aiSearchService;

        public ProductsController(
            ApplicationDbContext context,
            IAISearchService aiSearchService)
        {
            _context = context;
            _aiSearchService = aiSearchService;
        }

        // =========================================================
        // PRODUCT LIST / AI SMART SEARCH
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Index(
            string? search,
            int? categoryId)
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();

            var query = _context.Products
                .Include(p => p.Category)
                .Where(p =>
                    p.IsActive &&
                    p.StockQuantity > 0)
                .AsQueryable();

            bool aiFilterApplied = false;

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                try
                {
                    var aiQuery =
                        await _aiSearchService
                            .UnderstandSearchAsync(search);

                    var keyword =
                        aiQuery.Keyword?.Trim();

                    var category =
                        aiQuery.Category?.Trim();

                    // AI Category filter
                    if (!string.IsNullOrWhiteSpace(category))
                    {
                        query = query.Where(p =>
                            p.Category != null &&
                            p.Category.Name.Contains(category));

                        aiFilterApplied = true;

                        // AI Keyword filter
                        if (!string.IsNullOrWhiteSpace(keyword) &&
                            !string.Equals(
                                keyword,
                                category,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            query = query.Where(p =>
                                p.Name.Contains(keyword) ||
                                (p.Description != null &&
                                 p.Description.Contains(keyword)));
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(keyword))
                    {
                        query = query.Where(p =>
                            p.Name.Contains(keyword) ||
                            (p.Description != null &&
                             p.Description.Contains(keyword)) ||
                            (p.Category != null &&
                             p.Category.Name.Contains(keyword)));

                        aiFilterApplied = true;
                    }

                    // AI minimum price
                    if (aiQuery.MinPrice.HasValue)
                    {
                        query = query.Where(p =>
                            p.Price >= aiQuery.MinPrice.Value);

                        aiFilterApplied = true;
                    }

                    // AI maximum price
                    if (aiQuery.MaxPrice.HasValue)
                    {
                        query = query.Where(p =>
                            p.Price <= aiQuery.MaxPrice.Value);

                        aiFilterApplied = true;
                    }
                }
                catch
                {
                    aiFilterApplied = false;
                }

                /*
                 * If AI filtering was applied but produced
                 * no products, use local fallback search.
                 */
                if (aiFilterApplied)
                {
                    var aiProducts = await query
                        .AsNoTracking()
                        .ToListAsync();

                    if (aiProducts.Count == 0)
                    {
                        query = BuildFallbackSearchQuery(search);
                    }
                    else
                    {
                        var ids = aiProducts
                            .Select(p => p.Id)
                            .ToList();

                        query = _context.Products
                            .Include(p => p.Category)
                            .Where(p => ids.Contains(p.Id));
                    }
                }
                else
                {
                    query = BuildFallbackSearchQuery(search);
                }
            }

            // Category filter
            if (categoryId.HasValue)
            {
                query = query.Where(p =>
                    p.CategoryId == categoryId.Value);
            }

            var products = await query
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;
            ViewBag.Categories = categories;
            ViewBag.IsAISearch = aiFilterApplied;

            return View(products);
        }

        // =========================================================
        // LOCAL FALLBACK SEARCH
        // =========================================================
        private IQueryable<Models.Product>
            BuildFallbackSearchQuery(string search)
        {
            var cleanedSearch = search.Trim();

            decimal? maxPrice = null;
            decimal? minPrice = null;

            /*
             * Detect:
             * under 30000
             * below 30000
             * less than 30000
             */
            var maxPriceMatch = Regex.Match(
                cleanedSearch,
                @"(?:under|below|less\s+than)\s*৳?\s*(\d+(?:\.\d+)?)",
                RegexOptions.IgnoreCase);

            if (maxPriceMatch.Success &&
                decimal.TryParse(
                    maxPriceMatch.Groups[1].Value,
                    out var parsedMax))
            {
                maxPrice = parsedMax;

                cleanedSearch =
                    Regex.Replace(
                        cleanedSearch,
                        @"(?:under|below|less\s+than)\s*৳?\s*\d+(?:\.\d+)?",
                        "",
                        RegexOptions.IgnoreCase);
            }

            /*
             * Detect:
             * over 10000
             * above 10000
             * more than 10000
             */
            var minPriceMatch = Regex.Match(
                cleanedSearch,
                @"(?:over|above|more\s+than)\s*৳?\s*(\d+(?:\.\d+)?)",
                RegexOptions.IgnoreCase);

            if (minPriceMatch.Success &&
                decimal.TryParse(
                    minPriceMatch.Groups[1].Value,
                    out var parsedMin))
            {
                minPrice = parsedMin;

                cleanedSearch =
                    Regex.Replace(
                        cleanedSearch,
                        @"(?:over|above|more\s+than)\s*৳?\s*\d+(?:\.\d+)?",
                        "",
                        RegexOptions.IgnoreCase);
            }

            cleanedSearch =
                Regex.Replace(
                    cleanedSearch,
                    @"\s+",
                    " ")
                .Trim();

            var fallbackQuery = _context.Products
                .Include(p => p.Category)
                .Where(p =>
                    p.IsActive &&
                    p.StockQuantity > 0);

            if (!string.IsNullOrWhiteSpace(cleanedSearch))
            {
                fallbackQuery = fallbackQuery.Where(p =>
                    p.Name.Contains(cleanedSearch) ||
                    (p.Description != null &&
                     p.Description.Contains(cleanedSearch)) ||
                    (p.Category != null &&
                     p.Category.Name.Contains(cleanedSearch)));
            }

            if (minPrice.HasValue)
            {
                fallbackQuery = fallbackQuery.Where(p =>
                    p.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                fallbackQuery = fallbackQuery.Where(p =>
                    p.Price <= maxPrice.Value);
            }

            return fallbackQuery;
        }

        // =========================================================
        // PRODUCT DETAILS
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            // Load product with category
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p =>
                    p.Id == id &&
                    p.IsActive);

            if (product == null)
                return NotFound();

            // Keep existing out-of-stock behavior
            if (product.StockQuantity <= 0)
            {
                TempData["ErrorMessage"] =
                    "This product is currently out of stock.";

                return RedirectToAction(nameof(Index));
            }

            // =====================================================
            // PRODUCT REVIEWS
            // =====================================================

            // Load only approved reviews
            var reviews = await _context.ProductReviews
                .Include(r => r.User)
                .Where(r =>
                    r.ProductId == product.Id &&
                    r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            // Total reviews
            var reviewCount = reviews.Count;

            // Average rating
            var averageRating = reviewCount > 0
                ? reviews.Average(r => r.Rating)
                : 0;

            // =====================================================
            // RATING DISTRIBUTION
            // =====================================================

            var rating5Count = reviews.Count(r =>
                r.Rating == 5);

            var rating4Count = reviews.Count(r =>
                r.Rating == 4);

            var rating3Count = reviews.Count(r =>
                r.Rating == 3);

            var rating2Count = reviews.Count(r =>
                r.Rating == 2);

            var rating1Count = reviews.Count(r =>
                r.Rating == 1);

            // =====================================================
            // SEND REVIEW DATA TO VIEW
            // =====================================================

            ViewBag.Reviews = reviews;

            ViewBag.ReviewCount = reviewCount;

            ViewBag.AverageRating = averageRating;

            ViewBag.Rating5Count = rating5Count;
            ViewBag.Rating4Count = rating4Count;
            ViewBag.Rating3Count = rating3Count;
            ViewBag.Rating2Count = rating2Count;
            ViewBag.Rating1Count = rating1Count;

            return View(product);
        }
    }
}