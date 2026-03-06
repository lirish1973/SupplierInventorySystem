using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace SupplierInventorySystem.Services
{
    public class ImageService : IImageService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ImageService> _logger;

        private const int MaxWidth = 1920;
        private const int MaxHeight = 1920;
        private const int ThumbSize = 300;
        private const int JpegQuality = 75;
        private const long MaxFileSize = 5 * 1024 * 1024; // 5MB
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        public ImageService(IWebHostEnvironment env, ILogger<ImageService> logger)
        {
            _env = env;
            _logger = logger;
        }

        public async Task<(string filePath, string thumbPath, long fileSize)> SaveImageAsync(int productId, IFormFile file)
        {
            // Create directory
            var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "products", productId.ToString());
            Directory.CreateDirectory(uploadDir);

            // Generate unique filename
            var uniqueId = Guid.NewGuid().ToString("N")[..12];
            var fileName = $"{uniqueId}_full.jpg";
            var thumbName = $"{uniqueId}_thumb.jpg";
            var fullPath = Path.Combine(uploadDir, fileName);
            var thumbFullPath = Path.Combine(uploadDir, thumbName);

            try
            {
                using var stream = file.OpenReadStream();
                using var image = await Image.LoadAsync(stream);

                // Resize main image if needed (compress)
                if (image.Width > MaxWidth || image.Height > MaxHeight)
                {
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Max,
                        Size = new Size(MaxWidth, MaxHeight)
                    }));
                }

                // Save compressed full image as JPEG
                var encoder = new JpegEncoder { Quality = JpegQuality };
                await image.SaveAsJpegAsync(fullPath, encoder);

                // Create and save thumbnail
                using var thumbImage = image.Clone(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Crop,
                    Size = new Size(ThumbSize, ThumbSize)
                }));
                var thumbEncoder = new JpegEncoder { Quality = 70 };
                await thumbImage.SaveAsJpegAsync(thumbFullPath, thumbEncoder);

                var fileSize = new FileInfo(fullPath).Length;
                var relPath = $"/uploads/products/{productId}/{fileName}";
                var relThumbPath = $"/uploads/products/{productId}/{thumbName}";

                _logger.LogInformation("Image saved: {Path} ({Size} bytes)", relPath, fileSize);

                return (relPath, relThumbPath, fileSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save image for product {ProductId}", productId);
                // Cleanup on error
                if (File.Exists(fullPath)) File.Delete(fullPath);
                if (File.Exists(thumbFullPath)) File.Delete(thumbFullPath);
                throw;
            }
        }

        public Task<bool> DeleteImageFileAsync(string filePath, string? thumbPath)
        {
            try
            {
                var fullPath = Path.Combine(_env.WebRootPath, filePath.TrimStart('/'));
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }

                if (!string.IsNullOrEmpty(thumbPath))
                {
                    var thumbFullPath = Path.Combine(_env.WebRootPath, thumbPath.TrimStart('/'));
                    if (File.Exists(thumbFullPath))
                    {
                        File.Delete(thumbFullPath);
                    }
                }

                _logger.LogInformation("Image deleted: {Path}", filePath);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete image: {Path}", filePath);
                return Task.FromResult(false);
            }
        }

        public bool IsValidImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return false;

            if (file.Length > MaxFileSize)
                return false;

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                return false;

            return true;
        }

        public string GetAllowedExtensions() => string.Join(", ", AllowedExtensions);

        public long GetMaxFileSize() => MaxFileSize;
    }
}
