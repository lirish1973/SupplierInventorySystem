using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SupplierInventorySystem.Models
{
    [Table("supplier_addresses")]
    public class SupplierAddress
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("supplier_id")]
        public int SupplierId { get; set; }

        [Column("address_line1")]
        [StringLength(255)]
        [Display(Name = "כתובת שורה 1")]
        public string? AddressLine1 { get; set; }

        [Column("address_line2")]
        [StringLength(255)]
        [Display(Name = "כתובת שורה 2")]
        public string? AddressLine2 { get; set; }

        [Column("city")]
        [StringLength(100)]
        [Display(Name = "עיר")]
        public string? City { get; set; }

        [Column("region")]
        [StringLength(100)]
        [Display(Name = "אזור")]
        public string? Region { get; set; }

        [Column("postal_code")]
        [StringLength(20)]
        [Display(Name = "מיקוד")]
        public string? PostalCode { get; set; }

        [Column("country")]
        [StringLength(100)]
        [Display(Name = "מדינה")]
        public string? Country { get; set; }

        [Column("is_primary")]
        [Display(Name = "כתובת ראשית")]
        public bool IsPrimary { get; set; } = false;

        // Navigation properties
        [ForeignKey("SupplierId")]
        public Supplier? Supplier { get; set; }
    }
}