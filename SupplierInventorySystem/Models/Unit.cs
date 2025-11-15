using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SupplierInventorySystem.Models
{
    [Table("units")]
    public class Unit
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("code")]
        [Required]
        [StringLength(20)]
        [Display(Name = "קוד יחידה")]
        public string Code { get; set; } = string.Empty;

        [Column("description")]
        [StringLength(100)]
        [Display(Name = "תיאור")]
        public string? Description { get; set; }

        // Navigation properties
        public ICollection<Product>? Products { get; set; }
    }
}