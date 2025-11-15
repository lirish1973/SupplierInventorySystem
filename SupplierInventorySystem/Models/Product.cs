using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SupplierInventorySystem.Models
{
    [Table("products")]
    public class Product
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("sku")]
        [Required]
        [StringLength(100)]
        [Display(Name = "מק\"ט")]
        public string Sku { get; set; } = string.Empty;

        [Column("name")]
        [Required]
        [StringLength(255)]
        [Display(Name = "שם מוצר")]
        public string Name { get; set; } = string.Empty;

        [Column("description")]
        [Display(Name = "תיאור")]
        public string? Description { get; set; }

        [Column("category_id")]
        [Display(Name = "קטגוריה")]
        public int? CategoryId { get; set; }

        [Column("default_unit_id")]
        [Display(Name = "יחידת מידה")]
        public int? DefaultUnitId { get; set; }

        [Column("is_service")]
        [Display(Name = "שירות?")]
        public bool IsService { get; set; } = false;

        [Column("track_serials")]
        [Display(Name = "מעקב מספרים סידוריים")]
        public bool TrackSerials { get; set; } = false;

        [Column("track_lots")]
        [Display(Name = "מעקב אצוות")]
        public bool TrackLots { get; set; } = false;

        [Column("reorder_point")]
        [Display(Name = "נקודת הזמנה")]
        public decimal ReorderPoint { get; set; } = 0;

        [Column("reorder_qty")]
        [Display(Name = "כמות להזמנה")]
        public decimal ReorderQty { get; set; } = 0;

        [Column("active")]
        [Display(Name = "פעיל")]
        public bool Active { get; set; } = true;

        [Column("created_at")]
        [Display(Name = "תאריך יצירה")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updated_at")]
        [Display(Name = "תאריך עדכון")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("CategoryId")]
        public ProductCategory? Category { get; set; }

        [ForeignKey("DefaultUnitId")]
        public Unit? DefaultUnit { get; set; }

        public ICollection<ProductVariant>? ProductVariants { get; set; }
        public ICollection<SupplierProduct>? SupplierProducts { get; set; }
        public ICollection<ProductPriceHistory>? PriceHistories { get; set; }
    }
}