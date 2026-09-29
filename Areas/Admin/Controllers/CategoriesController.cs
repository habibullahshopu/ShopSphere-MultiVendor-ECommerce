using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;
using ShopSphere.Models;

namespace ShopSphere.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public CategoriesController(
     ApplicationDbContext context,
     IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }
        //GET: Admin/Categories
        public async Task<IActionResult> Index(string? search, string? status)
        {
            var query = _context.Categories.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c =>
                    c.Name.Contains(search) ||
                    (c.Description != null &&
                     c.Description.Contains(search)));
            }

            if (status == "Active")
            {
                query = query.Where(c => c.IsActive);
            }
            else if (status == "Inactive")
            {
                query = query.Where(c => !c.IsActive);
            }

            var categories = await query
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Status = status;

            return View(
                "~/Areas/Admin/Views/Categories/Index.cshtml",
                categories);
        }
        // GET: Admin/Categories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            if (ModelState.IsValid)
            {
                category.CreatedAt = DateTime.UtcNow;

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Category created successfully.";
                return RedirectToAction(nameof(Index));
            }

            return View(category);
        }

        // GET: Admin/Categories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var category = await _context.Categories.FindAsync(id);

            if (category == null)
                return NotFound();

            return View(category);
        }

        // POST: Admin/Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
    int id,
    Category category,
    IFormFile? imageFile)
        {
            if (id != category.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingCategory = await _context.Categories
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.Id == id);

                    if (existingCategory == null)
                        return NotFound();

                    // Keep existing image if no new image is selected
                    category.ImageUrl = existingCategory.ImageUrl;

                    // New image upload
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var allowedExtensions = new[]
                        {
                    ".jpg",
                    ".jpeg",
                    ".png",
                    ".webp"
                };

                        var extension = Path.GetExtension(imageFile.FileName)
                            .ToLowerInvariant();

                        if (!allowedExtensions.Contains(extension))
                        {
                            ModelState.AddModelError(
                                "ImageUrl",
                                "Only JPG, JPEG, PNG and WebP images are allowed.");

                            return View(category);
                        }

                        if (imageFile.Length > 5 * 1024 * 1024)
                        {
                            ModelState.AddModelError(
                                "ImageUrl",
                                "Image size must be less than 5 MB.");

                            return View(category);
                        }

                        var uploadFolder = Path.Combine(
                            _environment.WebRootPath,
                            "uploads",
                            "categories");

                        Directory.CreateDirectory(uploadFolder);

                        var fileName = $"{Guid.NewGuid()}{extension}";
                        var filePath = Path.Combine(uploadFolder, fileName);

                        using (var stream = new FileStream(
                            filePath,
                            FileMode.Create))
                        {
                            await imageFile.CopyToAsync(stream);
                        }

                        // Delete old image
                        if (!string.IsNullOrWhiteSpace(existingCategory.ImageUrl))
                        {
                            var oldImagePath = Path.Combine(
                                _environment.WebRootPath,
                                existingCategory.ImageUrl
                                    .TrimStart('/')
                                    .Replace('/', Path.DirectorySeparatorChar));

                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        category.ImageUrl = $"/uploads/categories/{fileName}";
                    }

                    // Preserve original creation date
                    category.CreatedAt = existingCategory.CreatedAt;

                    _context.Categories.Update(category);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] =
                        "Category updated successfully.";

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.Id))
                        return NotFound();

                    throw;
                }
            }

            return View(category);
        }

        // GET: Admin/Categories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return NotFound();

            return View(category);
        }

        // POST: Admin/Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories.FindAsync(id);

            if (category != null)
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Category deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(c => c.Id == id);
        }
    }
}