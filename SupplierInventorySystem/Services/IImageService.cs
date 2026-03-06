using Microsoft.AspNetCore.Http;

namespace SupplierInventorySystem.Services
{
    public interface IImageService
    {
        Task<(string filePath, string thumbPath, long fileSize)> SaveImageAsync(int productId, IFormFile file);
        Task<bool> DeleteImageFileAsync(string filePath, string? thumbPath);
        bool IsValidImage(IFormFile file);
        string GetAllowedExtensions();
        long GetMaxFileSize();
    }
}
