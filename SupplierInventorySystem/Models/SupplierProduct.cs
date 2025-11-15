using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SupplierInventorySystem.Models
{
    [Table("supplier_products")]
    public class SupplierProduct
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("supplier_id")]
        public int SupplierId { get; set; }

        [Column("product_id")]
        public int ProductId { get; set; }

        [Column("supplier_sku")]
        [StringLength(100)]
        [Display(Name = "מק\"ט ספק")]
        public string? SupplierSku { get; set; }

        [Column("lead_time_days")]
        [Display(Name = "זמן אספקה (ימים)")]
        public int? LeadTimeDays { get; set; }

        [Column("min_order_qty")]
        [Display(Name = "כמות הזמנה מינימלית")]
        public decimal? MinOrderQty { get; set; }

        [Column("price")]
        [Display(Name = "מחיר")]
        public decimal? Price { get; set; }

        [Column("currency")]
        [StringLength(3)]
        [Display(Name = "מטבע")]
        public string? Currency { get; set; }

        // Navigation properties
        [ForeignKey("SupplierId")]
        public Supplier? Supplier { get; set; }

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }
    }
}