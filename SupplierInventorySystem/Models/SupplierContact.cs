using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SupplierInventorySystem.Models
{
    [Table("supplier_contacts")]
    public class SupplierContact
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("supplier_id")]
        public int SupplierId { get; set; }

        [Column("name")]
        [StringLength(255)]
        [Display(Name = "שם איש קשר")]
        public string? Name { get; set; }

        [Column("role")]
        [StringLength(100)]
        [Display(Name = "תפקיד")]
        public string? Role { get; set; }

        [Column("phone")]
        [StringLength(50)]
        [Display(Name = "טלפון")]
        public string? Phone { get; set; }

        [Column("email")]
        [EmailAddress]
        [StringLength(255)]
        [Display(Name = "דוא\"ל")]
        public string? Email { get; set; }

        [Column("notes")]
        [Display(Name = "הערות")]
        public string? Notes { get; set; }

        // Navigation properties
        [ForeignKey("SupplierId")]
        public Supplier? Supplier { get; set; }
    }
}