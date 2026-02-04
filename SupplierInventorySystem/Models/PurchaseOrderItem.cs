using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SupplierInventorySystem.Models
{
    [Table("purchase_order_items")]
    public class PurchaseOrderItem
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("purchase_order_id")]
        [Required]
        public int PurchaseOrderId { get; set; }

        [Column("product_id")]
        [Required(ErrorMessage = "יש לבחור מוצר")]
        [Display(Name = "מוצר")]
        public int ProductId { get; set; }

        [Column("product_variant_id")]
        [Display(Name = "וריאנט")]
        public int? ProductVariantId { get; set; }

        [Column("description")]
        [StringLength(500)]
        [Display(Name = "תיאור")]
        public string? Description { get; set; }

        [Column("unit_id")]
        [Display(Name = "יחידה")]
        public int? UnitId { get; set; }

        [Column("quantity", TypeName = "decimal(10,4)")]
        [Required(ErrorMessage = "יש להזין כמות")]
        [Range(0.01, double.MaxValue, ErrorMessage = "הכמות חייבת להיות גדולה מאפס")]
        [Display(Name = "כמות")]
        public decimal Quantity { get; set; } = 1;

        [Column("unit_price", TypeName = "decimal(10,2)")]
        [Required(ErrorMessage = "יש להזין מחיר")]
        [Range(0, double.MaxValue, ErrorMessage = "המחיר חייב להיות חיובי")]
        [Display(Name = "מחיר יחידה")]
        public decimal UnitPrice { get; set; } = 0;

        [Column("discount_percent", TypeName = "decimal(5,2)")]
        [Range(0, 100)]
        [Display(Name = "הנחה %")]
        public decimal DiscountPercent { get; set; } = 0;

        [Column("line_total", TypeName = "decimal(10,2)")]
        [Display(Name = "סה\"כ שורה")]
        public decimal LineTotal { get; set; } = 0;

        [Column("quantity_received", TypeName = "decimal(10,4)")]
        [Display(Name = "כמות שהתקבלה")]
        public decimal QuantityReceived { get; set; } = 0;

        [Column("notes")]
        [Display(Name = "הערות")]
        public string? Notes { get; set; }

        // Navigation properties
        [ForeignKey("PurchaseOrderId")]
        public PurchaseOrder? PurchaseOrder { get; set; }

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        [ForeignKey("ProductVariantId")]
        public ProductVariant? ProductVariant { get; set; }

        [ForeignKey("UnitId")]
        public Unit? Unit { get; set; }

        // Calculated property
        [NotMapped]
        public decimal RemainingQuantity => Quantity - QuantityReceived;

        [NotMapped]
        public bool IsFullyReceived => QuantityReceived >= Quantity;

        // Calculate line total
        public void CalculateLineTotal()
        {
            var discountMultiplier = 1 - (DiscountPercent / 100);
            LineTotal = Math.Round(Quantity * UnitPrice * discountMultiplier, 2);
        }
    }
}
