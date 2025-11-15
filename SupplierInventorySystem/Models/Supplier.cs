using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SupplierInventorySystem.Models
{
    [Table("suppliers")]
    public class Supplier
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("code")]
        [StringLength(50)]
        [Display(Name = "קוד ספק")]
        public string? Code { get; set; }

        [Column("name")]
        [Required(ErrorMessage = "שם הספק הוא שדה חובה")]
        [StringLength(255)]
        [Display(Name = "שם הספק")]
        public string Name { get; set; } = string.Empty;

        [Column("legal_name")]
        [StringLength(255)]
        [Display(Name = "שם משפטי")]
        public string? LegalName { get; set; }

        [Column("tax_id")]
        [StringLength(50)]
        [Display(Name = "ח.פ / ע.מ")]
        public string? TaxId { get; set; }

        [Column("default_currency")]
        [StringLength(3)]
        [Display(Name = "מטבע")]
        public string DefaultCurrency { get; set; } = "ILS";

        [Column("default_payment_terms")]
        [StringLength(50)]
        [Display(Name = "תנאי תשלום")]
        public string? DefaultPaymentTerms { get; set; }

        [Column("lead_time_days")]
        [Display(Name = "זמן אספקה (ימים)")]
        public int? LeadTimeDays { get; set; }

        [Column("rating")]
        [Range(0, 5)]
        [Display(Name = "דירוג")]
        public decimal? Rating { get; set; }

        [Column("status")]
        [StringLength(20)]
        [Display(Name = "סטטוס")]
        public string Status { get; set; } = "active";

        [Column("created_at")]
        [Display(Name = "תאריך יצירה")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updated_at")]
        [Display(Name = "תאריך עדכון")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public ICollection<SupplierContact>? SupplierContacts { get; set; }
        public ICollection<SupplierAddress>? SupplierAddresses { get; set; }
        public ICollection<SupplierMetric>? SupplierMetrics { get; set; }
        public ICollection<SupplierProduct>? SupplierProducts { get; set; }
    }
}