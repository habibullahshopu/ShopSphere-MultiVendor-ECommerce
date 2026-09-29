using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;

namespace ShopSphere.Areas.Customer.Controllers
{
    [Area("Customer")]
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();

            var products = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.StockQuantity > 0)
                .OrderByDescending(p => p.CreatedAt)
                .Take(12)
                .ToListAsync();

            ViewBag.Categories = categories;
            ViewBag.Products = products;

            return View();
        }
    }
}