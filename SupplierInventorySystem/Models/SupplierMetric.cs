using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SupplierInventorySystem.Models
{
    [Table("supplier_metrics")]
    public class SupplierMetric
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("supplier_id")]
        public int SupplierId { get; set; }

        [Column("metric_date")]
        [Required]
        [Display(Name = "תאריך מדידה")]
        public DateTime MetricDate { get; set; }

        [Column("on_time_percentage")]
        [Range(0, 100)]
        [Display(Name = "אחוז אספקה בזמן")]
        public decimal? OnTimePercentage { get; set; }

        [Column("defect_rate")]
        [Range(0, 1)]
        [Display(Name = "שיעור פגמים")]
        public decimal? DefectRate { get; set; }

        [Column("notes")]
        [Display(Name = "הערות")]
        public string? Notes { get; set; }

        // Navigation properties
        [ForeignKey("SupplierId")]
        public Supplier? Supplier { get; set; }
    }
}