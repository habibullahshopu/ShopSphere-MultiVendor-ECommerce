using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;

namespace ShopSphere.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var totalProducts = await _context.Products.CountAsync();

            var activeProducts = await _context.Products
                .CountAsync(p => p.IsActive);

            var inactiveProducts = await _context.Products
                .CountAsync(p => !p.IsActive);

            var lowStockProducts = await _context.Products
                .CountAsync(p =>
                    p.StockQuantity > 0 &&
                    p.StockQuantity <= 10);

            var outOfStockProducts = await _context.Products
                .CountAsync(p => p.StockQuantity == 0);

            var totalCategories =
                await _context.Categories.CountAsync();

            var activeCategories =
                await _context.Categories
                    .CountAsync(c => c.IsActive);

            var totalUsers =
                await _context.Users.CountAsync();

            var recentProducts = await _context.Products
                .Include(p => p.Category)
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .ToListAsync();

            ViewBag.TotalProducts = totalProducts;
            ViewBag.ActiveProducts = activeProducts;
            ViewBag.InactiveProducts = inactiveProducts;

            ViewBag.LowStockProducts = lowStockProducts;
            ViewBag.OutOfStockProducts = outOfStockProducts;

            ViewBag.TotalCategories = totalCategories;
            ViewBag.ActiveCategories = activeCategories;

            ViewBag.TotalUsers = totalUsers;

            ViewBag.RecentProducts = recentProducts;

            return View();
        }
    }
}