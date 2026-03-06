using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SupplierInventorySystem.Models
{
    [Table("product_images")]
    public class ProductImage
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("product_id")]
        [Display(Name = "מוצר")]
        public int ProductId { get; set; }

        [Column("file_name")]
        [StringLength(255)]
        [Display(Name = "שם קובץ")]
        public string FileName { get; set; } = string.Empty;

        [Column("original_file_name")]
        [StringLength(255)]
        [Display(Name = "שם קובץ מקורי")]
        public string OriginalFileName { get; set; } = string.Empty;

        [Column("file_path")]
        [StringLength(500)]
        [Display(Name = "נתיב קובץ")]
        public string FilePath { get; set; } = string.Empty;

        [Column("thumb_path")]
        [StringLength(500)]
        [Display(Name = "נתיב תמונה ממוזערת")]
        public string? ThumbPath { get; set; }

        [Column("file_size")]
        [Display(Name = "גודל קובץ")]
        public long FileSize { get; set; }

        [Column("is_primary")]
        [Display(Name = "תמונה ראשית")]
        public bool IsPrimary { get; set; } = false;

        [Column("display_order")]
        [Display(Name = "סדר תצוגה")]
        public int DisplayOrder { get; set; } = 0;

        [Column("uploaded_at")]
        [Display(Name = "תאריך העלאה")]
        public DateTime UploadedAt { get; set; } = DateTime.Now;

        // Navigation property
        [ForeignKey("ProductId")]
        public Product? Product { get; set; }
    }
}
