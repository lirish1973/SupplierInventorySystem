using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SupplierInventorySystem.Models
{
    [Table("purchase_orders")]
    public class PurchaseOrder
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("order_number")]
        [Required]
        [StringLength(50)]
        [Display(Name = "מספר הזמנה")]
        public string OrderNumber { get; set; } = string.Empty;

        [Column("supplier_id")]
        [Required(ErrorMessage = "יש לבחור ספק")]
        [Display(Name = "ספק")]
        public int SupplierId { get; set; }

        [Column("order_date")]
        [Required]
        [Display(Name = "תאריך הזמנה")]
        [DataType(DataType.Date)]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Column("expected_delivery_date")]
        [Display(Name = "תאריך אספקה צפוי")]
        [DataType(DataType.Date)]
        public DateTime? ExpectedDeliveryDate { get; set; }

        [Column("actual_delivery_date")]
        [Display(Name = "תאריך אספקה בפועל")]
        [DataType(DataType.Date)]
        public DateTime? ActualDeliveryDate { get; set; }

        [Column("status")]
        [Required]
        [StringLength(20)]
        [Display(Name = "סטטוס")]
        public string Status { get; set; } = "Draft"; // Draft, Sent, Confirmed, Shipped, Received, Cancelled

        [Column("subtotal", TypeName = "decimal(10,2)")]
        [Display(Name = "סכום ביניים")]
        public decimal Subtotal { get; set; } = 0;

        [Column("tax_amount", TypeName = "decimal(10,2)")]
        [Display(Name = "מע\"מ")]
        public decimal TaxAmount { get; set; } = 0;

        [Column("discount_amount", TypeName = "decimal(10,2)")]
        [Display(Name = "הנחה")]
        public decimal DiscountAmount { get; set; } = 0;

        [Column("shipping_cost", TypeName = "decimal(10,2)")]
        [Display(Name = "עלות משלוח")]
        public decimal ShippingCost { get; set; } = 0;

        [Column("total_amount", TypeName = "decimal(10,2)")]
        [Display(Name = "סה\"כ")]
        public decimal TotalAmount { get; set; } = 0;

        [Column("currency")]
        [StringLength(3)]
        [Display(Name = "מטבע")]
        public string Currency { get; set; } = "ILS";

        [Column("payment_terms")]
        [StringLength(100)]
        [Display(Name = "תנאי תשלום")]
        public string? PaymentTerms { get; set; }

        [Column("notes")]
        [Display(Name = "הערות")]
        public string? Notes { get; set; }

        [Column("internal_notes")]
        [Display(Name = "הערות פנימיות")]
        public string? InternalNotes { get; set; }

        [Column("created_by")]
        [Display(Name = "נוצר על ידי")]
        public int? CreatedById { get; set; }

        [Column("created_at")]
        [Display(Name = "תאריך יצירה")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updated_at")]
        [Display(Name = "תאריך עדכון")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("SupplierId")]
        public Supplier? Supplier { get; set; }

        [ForeignKey("CreatedById")]
        public User? CreatedBy { get; set; }

        public ICollection<PurchaseOrderItem>? Items { get; set; }
    }

    // סטטוסים אפשריים להזמנה
    public static class PurchaseOrderStatus
    {
        public const string Draft = "Draft";           // טיוטה
        public const string Sent = "Sent";             // נשלח לספק
        public const string Confirmed = "Confirmed";   // אושר על ידי הספק
        public const string Shipped = "Shipped";       // נשלח
        public const string PartiallyReceived = "PartiallyReceived"; // התקבל חלקית
        public const string Received = "Received";     // התקבל במלואו
        public const string Cancelled = "Cancelled";   // בוטל

        public static Dictionary<string, string> GetStatusDisplayNames()
        {
            return new Dictionary<string, string>
            {
                { Draft, "טיוטה" },
                { Sent, "נשלח לספק" },
                { Confirmed, "אושר" },
                { Shipped, "במשלוח" },
                { PartiallyReceived, "התקבל חלקית" },
                { Received, "התקבל" },
                { Cancelled, "בוטל" }
            };
        }

        public static string GetDisplayName(string status)
        {
            var names = GetStatusDisplayNames();
            return names.TryGetValue(status, out var name) ? name : status;
        }

        public static string GetStatusBadgeClass(string status)
        {
            return status switch
            {
                Draft => "bg-secondary",
                Sent => "bg-info",
                Confirmed => "bg-primary",
                Shipped => "bg-warning",
                PartiallyReceived => "bg-warning",
                Received => "bg-success",
                Cancelled => "bg-danger",
                _ => "bg-secondary"
            };
        }
    }
}
