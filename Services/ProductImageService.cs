using Microsoft.AspNetCore.Http;

namespace ShopSphere.Services
{
    public class ProductImageService : IProductImageService
    {
        private readonly IWebHostEnvironment _environment;

        private static readonly string[] AllowedExtensions =
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

        private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

        public ProductImageService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string?> SaveImageAsync(IFormFile? imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
                return null;

            if (imageFile.Length > MaxFileSize)
                throw new InvalidOperationException(
                    "Image size must not exceed 5 MB.");

            var extension = Path.GetExtension(imageFile.FileName)
                .ToLowerInvariant();

            if (!AllowedExtensions.Contains(extension))
                throw new InvalidOperationException(
                    "Only JPG, JPEG, PNG and WebP images are allowed.");

            var uploadsFolder = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "products");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName =
                $"{Guid.NewGuid():N}{extension}";

            var filePath = Path.Combine(
                uploadsFolder,
                uniqueFileName);

            await using var stream = new FileStream(
                filePath,
                FileMode.Create);

            await imageFile.CopyToAsync(stream);

            return $"/uploads/products/{uniqueFileName}";
        }

        public Task DeleteImageAsync(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return Task.CompletedTask;

            var fileName = Path.GetFileName(imageUrl);

            if (string.IsNullOrWhiteSpace(fileName))
                return Task.CompletedTask;

            var filePath = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "products",
                fileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            return Task.CompletedTask;
        }
    }
}