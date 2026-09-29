namespace ShopSphere.Services
{
    public interface IProductImageService
    {
        Task<string?> SaveImageAsync(IFormFile? imageFile);
        Task DeleteImageAsync(string? imageUrl);
    }
}