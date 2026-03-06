using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SupplierInventorySystem.Data;
using SupplierInventorySystem.Models;
using SupplierInventorySystem.Services;

namespace SupplierInventorySystem.Controllers
{
    [Authorize]
    public class ProductImagesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IImageService _imageService;

        public ProductImagesController(ApplicationDbContext context, IImageService imageService)
        {
            _context = context;
            _imageService = imageService;
        }

        // GET: ProductImages/GetProductImages?productId=5
        [HttpGet]
        public async Task<IActionResult> GetProductImages(int productId)
        {
            var images = await _context.ProductImages
                .Where(pi => pi.ProductId == productId)
                .OrderBy(pi => pi.DisplayOrder)
                .Select(pi => new
                {
                    pi.Id,
                    pi.FileName,
                    pi.OriginalFileName,
                    pi.FilePath,
                    pi.ThumbPath,
                    pi.FileSize,
                    pi.IsPrimary,
                    pi.DisplayOrder,
                    UploadedAt = pi.UploadedAt.ToString("dd/MM/yyyy HH:mm")
                })
                .ToListAsync();

            return Json(new { success = true, images });
        }

        // POST: ProductImages/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(int productId, List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
            {
                return Json(new { success = false, message = "לא נבחרו קבצים להעלאה" });
            }

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return Json(new { success = false, message = "מוצר לא נמצא" });
            }

            var uploadedImages = new List<object>();
            var errors = new List<string>();

            // Get current max display order
            var maxOrder = await _context.ProductImages
                .Where(pi => pi.ProductId == productId)
                .MaxAsync(pi => (int?)pi.DisplayOrder) ?? 0;

            // Check if product has any images
            var hasExistingImages = await _context.ProductImages
                .AnyAsync(pi => pi.ProductId == productId);

            foreach (var file in files)
            {
                if (!_imageService.IsValidImage(file))
                {
                    errors.Add($"הקובץ '{file.FileName}' אינו תקין. נתמכים: {_imageService.GetAllowedExtensions()}, מקס: {_imageService.GetMaxFileSize() / 1024 / 1024}MB");
                    continue;
                }

                try
                {
                    var (filePath, thumbPath, fileSize) = await _imageService.SaveImageAsync(productId, file);

                    maxOrder++;
                    var productImage = new ProductImage
                    {
                        ProductId = productId,
                        FileName = Path.GetFileName(filePath),
                        OriginalFileName = file.FileName,
                        FilePath = filePath,
                        ThumbPath = thumbPath,
                        FileSize = fileSize,
                        IsPrimary = !hasExistingImages && uploadedImages.Count == 0, // First image is primary
                        DisplayOrder = maxOrder,
                        UploadedAt = DateTime.Now
                    };

                    _context.ProductImages.Add(productImage);
                    await _context.SaveChangesAsync();

                    uploadedImages.Add(new
                    {
                        productImage.Id,
                        productImage.FileName,
                        productImage.OriginalFileName,
                        productImage.FilePath,
                        productImage.ThumbPath,
                        productImage.FileSize,
                        productImage.IsPrimary,
                        productImage.DisplayOrder,
                        UploadedAt = productImage.UploadedAt.ToString("dd/MM/yyyy HH:mm")
                    });
                }
                catch (Exception)
                {
                    errors.Add($"שגיאה בהעלאת הקובץ '{file.FileName}'");
                }
            }

            return Json(new
            {
                success = uploadedImages.Count > 0,
                images = uploadedImages,
                errors,
                message = uploadedImages.Count > 0
                    ? $"{uploadedImages.Count} תמונות הועלו בהצלחה"
                    : "לא הועלו תמונות"
            });
        }

        // POST: ProductImages/DeleteImage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int imageId)
        {
            var image = await _context.ProductImages.FindAsync(imageId);
            if (image == null)
            {
                return Json(new { success = false, message = "תמונה לא נמצאה" });
            }

            // Delete file from disk
            await _imageService.DeleteImageFileAsync(image.FilePath, image.ThumbPath);

            var wasPrimary = image.IsPrimary;
            var productId = image.ProductId;

            _context.ProductImages.Remove(image);
            await _context.SaveChangesAsync();

            // If deleted image was primary, set the first remaining image as primary
            if (wasPrimary)
            {
                var firstImage = await _context.ProductImages
                    .Where(pi => pi.ProductId == productId)
                    .OrderBy(pi => pi.DisplayOrder)
                    .FirstOrDefaultAsync();

                if (firstImage != null)
                {
                    firstImage.IsPrimary = true;
                    await _context.SaveChangesAsync();
                }
            }

            return Json(new { success = true, message = "התמונה נמחקה בהצלחה" });
        }

        // POST: ProductImages/DeleteMultiple
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMultiple([FromBody] DeleteMultipleRequest request)
        {
            if (request?.ImageIds == null || request.ImageIds.Count == 0)
            {
                return Json(new { success = false, message = "לא נבחרו תמונות למחיקה" });
            }

            var images = await _context.ProductImages
                .Where(pi => request.ImageIds.Contains(pi.Id))
                .ToListAsync();

            if (images.Count == 0)
            {
                return Json(new { success = false, message = "תמונות לא נמצאו" });
            }

            var productId = images.First().ProductId;
            var deletedPrimary = images.Any(i => i.IsPrimary);

            foreach (var image in images)
            {
                await _imageService.DeleteImageFileAsync(image.FilePath, image.ThumbPath);
                _context.ProductImages.Remove(image);
            }

            await _context.SaveChangesAsync();

            // If primary was deleted, set new primary
            if (deletedPrimary)
            {
                var firstImage = await _context.ProductImages
                    .Where(pi => pi.ProductId == productId)
                    .OrderBy(pi => pi.DisplayOrder)
                    .FirstOrDefaultAsync();

                if (firstImage != null)
                {
                    firstImage.IsPrimary = true;
                    await _context.SaveChangesAsync();
                }
            }

            return Json(new { success = true, message = $"{images.Count} תמונות נמחקו בהצלחה" });
        }

        // POST: ProductImages/SetPrimary
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPrimary(int productId, int imageId)
        {
            // Remove current primary
            var currentPrimary = await _context.ProductImages
                .Where(pi => pi.ProductId == productId && pi.IsPrimary)
                .ToListAsync();

            foreach (var img in currentPrimary)
            {
                img.IsPrimary = false;
            }

            // Set new primary
            var newPrimary = await _context.ProductImages.FindAsync(imageId);
            if (newPrimary == null || newPrimary.ProductId != productId)
            {
                return Json(new { success = false, message = "תמונה לא נמצאה" });
            }

            newPrimary.IsPrimary = true;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "התמונה הראשית עודכנה" });
        }
    }

    public class DeleteMultipleRequest
    {
        public List<int> ImageIds { get; set; } = new();
    }
}
