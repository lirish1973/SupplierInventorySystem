using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SupplierInventorySystem.Models
{
    [Table("product_price_history")]
    public class ProductPriceHistory
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("product_id")]
        public int ProductId { get; set; }

        [Column("supplier_id")]
        public int? SupplierId { get; set; }

        [Column("price")]
        [Required]
        [Display(Name = "מחיר")]
        public decimal Price { get; set; }

        [Column("currency")]
        [StringLength(3)]
        [Display(Name = "מטבע")]
        public string? Currency { get; set; }

        [Column("effective_from")]
        [Display(Name = "בתוקף מתאריך")]
        public DateTime? EffectiveFrom { get; set; }

        [Column("effective_to")]
        [Display(Name = "בתוקף עד תאריך")]
        public DateTime? EffectiveTo { get; set; }

        // Navigation properties
        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        [ForeignKey("SupplierId")]
        public Supplier? Supplier { get; set; }
    }
}