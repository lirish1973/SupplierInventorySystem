using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SupplierInventorySystem.Models
{
    [Table("product_variants")]
    public class ProductVariant
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("product_id")]
        public int ProductId { get; set; }

        [Column("variant_sku")]
        [StringLength(100)]
        [Display(Name = "מק\"ט וריאנט")]
        public string? VariantSku { get; set; }

        [Column("attributes", TypeName = "json")]
        [Display(Name = "מאפיינים")]
        public string? Attributes { get; set; }

        [Column("active")]
        [Display(Name = "פעיל")]
        public bool Active { get; set; } = true;

        // Navigation properties
        [ForeignKey("ProductId")]
        public Product? Product { get; set; }
    }
}