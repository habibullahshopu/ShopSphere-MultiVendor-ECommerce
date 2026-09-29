using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopSphere.Data;
using ShopSphere.Models;
using ShopSphere.Services;

namespace ShopSphere.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IProductImageService _productImageService;

        public ProductsController(
            ApplicationDbContext context,
            IProductImageService productImageService)
        {
            _context = context;
            _productImageService = productImageService;
        }

        // =========================================================
        // GET: Admin/Products
        // =========================================================
        public async Task<IActionResult> Index(
            string? search,
            int? categoryId,
            string? status,
            string? stock,
            string? sort)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .AsQueryable();

            // 🔎 Search
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(p =>
                    p.Name.Contains(search) ||
                    (p.Description != null &&
                     p.Description.Contains(search)) ||
                    (p.Category != null &&
                     p.Category.Name.Contains(search))
                );
            }

            // 📁 Category Filter
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(
                    p => p.CategoryId == categoryId.Value);
            }

            // 🟢 Status Filter
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status == "active")
                {
                    query = query.Where(p => p.IsActive);
                }
                else if (status == "inactive")
                {
                    query = query.Where(p => !p.IsActive);
                }
            }

            // 📦 Stock Filter
            if (!string.IsNullOrWhiteSpace(stock))
            {
                if (stock == "instock")
                {
                    query = query.Where(
                        p => p.StockQuantity > 10);
                }
                else if (stock == "lowstock")
                {
                    query = query.Where(
                        p => p.StockQuantity > 0 &&
                             p.StockQuantity <= 10);
                }
                else if (stock == "outofstock")
                {
                    query = query.Where(
                        p => p.StockQuantity == 0);
                }
            }

            // ↕️ Sorting
            query = sort switch
            {
                "oldest" =>
                    query.OrderBy(p => p.CreatedAt),

                "price_asc" =>
                    query.OrderBy(p => p.Price),

                "price_desc" =>
                    query.OrderByDescending(p => p.Price),

                "name_asc" =>
                    query.OrderBy(p => p.Name),

                "name_desc" =>
                    query.OrderByDescending(p => p.Name),

                _ =>
                    query.OrderByDescending(p => p.CreatedAt)
            };

            var products = await query.ToListAsync();

            // Send filter values back to View
            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;
            ViewBag.Status = status;
            ViewBag.Stock = stock;
            ViewBag.Sort = sort;

            // Categories for filter dropdown
            ViewBag.Categories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(products);
        }

        // =========================================================
        // GET: Admin/Products/Create
        // =========================================================
        public async Task<IActionResult> Create()
        {
            await EnsureDefaultCategoriesAsync();
            await LoadCategoriesAsync();

            return View();
        }

        // =========================================================
        // POST: Admin/Products/Create
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Product product,
            IFormFile? imageFile)
        {
            // Check if selected category exists and is active
            var categoryExists = await _context.Categories
                .AnyAsync(c =>
                    c.Id == product.CategoryId &&
                    c.IsActive);

            if (!categoryExists)
            {
                ModelState.AddModelError(
                    "CategoryId",
                    "Please select a valid category."
                );
            }

            if (ModelState.IsValid)
            {
                string? uploadedImageUrl = null;

                try
                {
                    product.CreatedAt = DateTime.UtcNow;

                    // Save uploaded product image
                    if (imageFile != null &&
                        imageFile.Length > 0)
                    {
                        uploadedImageUrl =
                            await _productImageService
                                .SaveImageAsync(imageFile);

                        product.ImageUrl =
                            uploadedImageUrl;
                    }

                    _context.Products.Add(product);

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] =
                        "Product created successfully.";

                    return RedirectToAction(nameof(Index));
                }
                catch (InvalidOperationException ex)
                {
                    // If database save fails after image upload,
                    // remove the newly uploaded image.
                    if (!string.IsNullOrWhiteSpace(
                        uploadedImageUrl))
                    {
                        await _productImageService
                            .DeleteImageAsync(uploadedImageUrl);
                    }

                    ModelState.AddModelError(
                        "imageFile",
                        ex.Message);
                }
                catch
                {
                    // Cleanup uploaded file if an unexpected
                    // error occurs.
                    if (!string.IsNullOrWhiteSpace(
                        uploadedImageUrl))
                    {
                        await _productImageService
                            .DeleteImageAsync(uploadedImageUrl);
                    }

                    throw;
                }
            }

            await LoadCategoriesAsync();

            return View(product);
        }

        // =========================================================
        // GET: Admin/Products/Edit/5
        // =========================================================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var product = await _context.Products
                .FindAsync(id);

            if (product == null)
                return NotFound();

            await LoadCategoriesAsync();

            return View(product);
        }

        // =========================================================
        // POST: Admin/Products/Edit/5
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Product product,
            IFormFile? imageFile)
        {
            if (id != product.Id)
                return NotFound();

            var categoryExists = await _context.Categories
                .AnyAsync(c =>
                    c.Id == product.CategoryId &&
                    c.IsActive);

            if (!categoryExists)
            {
                ModelState.AddModelError(
                    "CategoryId",
                    "Please select a valid category."
                );
            }

            if (ModelState.IsValid)
            {
                string? newImageUrl = null;

                try
                {
                    // Load existing product from database
                    var existingProduct =
                        await _context.Products.FindAsync(id);

                    if (existingProduct == null)
                        return NotFound();

                    // Keep original image URL
                    var oldImageUrl =
                        existingProduct.ImageUrl;

                    // Update normal product fields
                    existingProduct.Name =
                        product.Name;

                    existingProduct.Description =
                        product.Description;

                    existingProduct.Price =
                        product.Price;

                    existingProduct.StockQuantity =
                        product.StockQuantity;

                    existingProduct.IsActive =
                        product.IsActive;

                    existingProduct.CategoryId =
                        product.CategoryId;

                    // =================================================
                    // Image replacement
                    // =================================================
                    if (imageFile != null &&
                        imageFile.Length > 0)
                    {
                        // Save new image first
                        newImageUrl =
                            await _productImageService
                                .SaveImageAsync(imageFile);

                        // Update database with new image path
                        existingProduct.ImageUrl =
                            newImageUrl;
                    }
                    else
                    {
                        // No new image:
                        // Keep existing image.
                        existingProduct.ImageUrl =
                            oldImageUrl;
                    }

                    await _context.SaveChangesAsync();

                    // =================================================
                    // Delete old image AFTER successful DB update
                    // =================================================
                    if (!string.IsNullOrWhiteSpace(newImageUrl) &&
                        !string.IsNullOrWhiteSpace(oldImageUrl))
                    {
                        // Only delete files belonging to our
                        // uploads/products folder.
                        if (oldImageUrl.StartsWith(
                            "/uploads/products/",
                            StringComparison.OrdinalIgnoreCase))
                        {
                            await _productImageService
                                .DeleteImageAsync(oldImageUrl);
                        }
                    }

                    TempData["SuccessMessage"] =
                        "Product updated successfully.";

                    return RedirectToAction(nameof(Index));
                }
                catch (InvalidOperationException ex)
                {
                    // New image may already have been saved.
                    // Remove it if the update fails.
                    if (!string.IsNullOrWhiteSpace(
                        newImageUrl))
                    {
                        await _productImageService
                            .DeleteImageAsync(newImageUrl);
                    }

                    ModelState.AddModelError(
                        "imageFile",
                        ex.Message);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
                        return NotFound();

                    throw;
                }
            }

            await LoadCategoriesAsync();

            return View(product);
        }

        // =========================================================
        // GET: Admin/Products/Delete/5
        // =========================================================
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound();

            return View(product);
        }

        // =========================================================
        // POST: Admin/Products/Delete/5
        // =========================================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products
                .FindAsync(id);

            if (product == null)
            {
                return RedirectToAction(nameof(Index));
            }

            // =========================================================
            // IMPORTANT:
            // A product that is already connected to order history,
            // reviews, or wishlist records must not be hard deleted.
            //
            // Otherwise SQL Server will block the DELETE because
            // related records still reference ProductId.
            // =========================================================

            var hasOrderHistory = await _context.OrderItems
                .AnyAsync(oi => oi.ProductId == id);

            var hasReviews = await _context.ProductReviews
                .AnyAsync(r => r.ProductId == id);

            var hasWishlist = await _context.Wishlists
                .AnyAsync(w => w.ProductId == id);

            if (hasOrderHistory || hasReviews || hasWishlist)
            {
                // SAFE DELETE:
                // Keep product and historical references,
                // but make it unavailable in the shop.
                product.IsActive = false;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Product has been deactivated because it is already connected to existing records.";

                return RedirectToAction(nameof(Index));
            }

            // =========================================================
            // HARD DELETE
            // Only products with no related records are physically
            // removed from the database.
            // =========================================================

            // Delete product image from storage
            if (!string.IsNullOrWhiteSpace(
                product.ImageUrl))
            {
                // Only delete local uploaded images.
                if (product.ImageUrl.StartsWith(
                    "/uploads/products/",
                    StringComparison.OrdinalIgnoreCase))
                {
                    await _productImageService
                        .DeleteImageAsync(
                            product.ImageUrl);
                }
            }

            _context.Products.Remove(product);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Product deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // Load Active Categories for Product Dropdown
        // =========================================================
        private async Task LoadCategoriesAsync()
        {
            ViewBag.Categories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        // =========================================================
        // Add Default Professional Categories
        // =========================================================
        private async Task EnsureDefaultCategoriesAsync()
        {
            var defaultCategories = new List<Category>
            {
                new Category
                {
                    Name = "Electronics",
                    Description =
                        "Mobile phones, computers, gadgets and electronic accessories.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },

                new Category
                {
                    Name = "Fashion",
                    Description =
                        "Clothing, shoes, bags and fashion accessories.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },

                new Category
                {
                    Name = "Home & Living",
                    Description =
                        "Furniture, home decor, kitchen and household products.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },

                new Category
                {
                    Name = "Beauty & Personal Care",
                    Description =
                        "Skincare, cosmetics, grooming and personal care products.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },

                new Category
                {
                    Name = "Groceries",
                    Description =
                        "Food, beverages and everyday grocery essentials.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },

                new Category
                {
                    Name = "Sports & Fitness",
                    Description =
                        "Sports equipment, fitness gear and active lifestyle products.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },

                new Category
                {
                    Name = "Books & Stationery",
                    Description =
                        "Books, educational materials and office supplies.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },

                new Category
                {
                    Name = "Gaming",
                    Description =
                        "Gaming consoles, games and gaming accessories.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },

                new Category
                {
                    Name = "Automotive",
                    Description =
                        "Vehicle accessories, tools and automotive products.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },

                new Category
                {
                    Name = "Pet Supplies",
                    Description =
                        "Pet food, toys, accessories and pet care products.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            var existingCategories =
                await _context.Categories
                    .ToListAsync();

            // Make existing categories active
            foreach (var existingCategory
                     in existingCategories)
            {
                if (!existingCategory.IsActive)
                {
                    existingCategory.IsActive = true;
                }
            }

            // Get existing category names
            var existingNames = existingCategories
                .Select(c => c.Name.Trim().ToLower())
                .ToHashSet();

            // Find missing default categories
            var missingCategories =
                defaultCategories
                    .Where(c =>
                        !existingNames.Contains(
                            c.Name.Trim().ToLower()))
                    .ToList();

            if (missingCategories.Any())
            {
                _context.Categories
                    .AddRange(missingCategories);
            }

            await _context.SaveChangesAsync();
        }

        // =========================================================
        // Check Product Exists
        // =========================================================
        private bool ProductExists(int id)
        {
            return _context.Products
                .Any(p => p.Id == id);
        }
    }
}