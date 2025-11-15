using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SupplierInventorySystem.Models
{
    [Table("product_categories")]
    public class ProductCategory
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        [Required]
        [StringLength(255)]
        [Display(Name = "שם קטגוריה")]
        public string Name { get; set; } = string.Empty;

        [Column("parent_id")]
        [Display(Name = "קטגוריית אב")]
        public int? ParentId { get; set; }

        // Navigation properties
        [ForeignKey("ParentId")]
        public ProductCategory? ParentCategory { get; set; }

        public ICollection<ProductCategory>? SubCategories { get; set; }
        public ICollection<Product>? Products { get; set; }
    }
}