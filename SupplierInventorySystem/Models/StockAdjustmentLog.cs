using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SupplierInventorySystem.Models
{
    [Table("stock_adjustment_logs")]
    public class StockAdjustmentLog
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("product_id")]
        [Required]
        public int ProductId { get; set; }

        [Column("quantity_change", TypeName = "decimal(10,4)")]
        [Required]
        [Display(Name = "שינוי כמות")]
        public decimal QuantityChange { get; set; }

        [Column("quantity_before", TypeName = "decimal(10,4)")]
        [Display(Name = "כמות לפני")]
        public decimal QuantityBefore { get; set; }

        [Column("quantity_after", TypeName = "decimal(10,4)")]
        [Display(Name = "כמות אחרי")]
        public decimal QuantityAfter { get; set; }

        [Column("reason")]
        [Required]
        [StringLength(255)]
        [Display(Name = "סיבה")]
        public string Reason { get; set; } = string.Empty;

        [Column("adjusted_by")]
        [Display(Name = "בוצע על ידי")]
        public int? AdjustedById { get; set; }

        [Column("adjusted_at")]
        [Display(Name = "תאריך")]
        public DateTime AdjustedAt { get; set; } = DateTime.Now;

        // Navigation
        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        [ForeignKey("AdjustedById")]
        public User? AdjustedBy { get; set; }
    }
}
