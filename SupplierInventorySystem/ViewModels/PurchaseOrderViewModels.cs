using System.ComponentModel.DataAnnotations;
using SupplierInventorySystem.Models;

namespace SupplierInventorySystem.ViewModels
{
    // ViewModel for purchase order list
    public class PurchaseOrderListViewModel
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; } = "ILS";
        public int ItemCount { get; set; }
        public string? CreatedByName { get; set; }
    }

    // ViewModel for creating/editing purchase order
    public class PurchaseOrderFormViewModel
    {
        public int Id { get; set; }

        [Display(Name = "מספר הזמנה")]
        public string? OrderNumber { get; set; }

        [Required(ErrorMessage = "יש לבחור ספק")]
        [Display(Name = "ספק")]
        public int SupplierId { get; set; }

        [Required]
        [Display(Name = "תאריך הזמנה")]
        [DataType(DataType.Date)]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Display(Name = "תאריך אספקה צפוי")]
        [DataType(DataType.Date)]
        public DateTime? ExpectedDeliveryDate { get; set; }

        [Display(Name = "תנאי תשלום")]
        public string? PaymentTerms { get; set; }

        [Display(Name = "מטבע")]
        public string Currency { get; set; } = "ILS";

        [Display(Name = "הערות לספק")]
        public string? Notes { get; set; }

        [Display(Name = "הערות פנימיות")]
        public string? InternalNotes { get; set; }

        // Items
        public List<PurchaseOrderItemFormViewModel> Items { get; set; } = new();

        // Summary
        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal TotalAmount { get; set; }
    }

    // ViewModel for order item in form
    public class PurchaseOrderItemFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "יש לבחור מוצר")]
        [Display(Name = "מוצר")]
        public int ProductId { get; set; }

        [Display(Name = "שם מוצר")]
        public string? ProductName { get; set; }

        [Display(Name = "מק\"ט")]
        public string? ProductSku { get; set; }

        [Display(Name = "תיאור")]
        public string? Description { get; set; }

        [Display(Name = "יחידה")]
        public int? UnitId { get; set; }

        [Display(Name = "יחידה")]
        public string? UnitCode { get; set; }

        [Required(ErrorMessage = "יש להזין כמות")]
        [Range(0.01, double.MaxValue, ErrorMessage = "הכמות חייבת להיות גדולה מאפס")]
        [Display(Name = "כמות")]
        public decimal Quantity { get; set; } = 1;

        [Required(ErrorMessage = "יש להזין מחיר")]
        [Range(0, double.MaxValue, ErrorMessage = "המחיר חייב להיות חיובי")]
        [Display(Name = "מחיר יחידה")]
        public decimal UnitPrice { get; set; } = 0;

        [Range(0, 100)]
        [Display(Name = "הנחה %")]
        public decimal DiscountPercent { get; set; } = 0;

        [Display(Name = "סה\"כ שורה")]
        public decimal LineTotal { get; set; } = 0;

        [Display(Name = "הערות")]
        public string? Notes { get; set; }
    }

    // ViewModel for order details view
    public class PurchaseOrderDetailsViewModel
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public Supplier? Supplier { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        public DateTime? ActualDeliveryDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusDisplayName => PurchaseOrderStatus.GetDisplayName(Status);
        public string StatusBadgeClass => PurchaseOrderStatus.GetStatusBadgeClass(Status);
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; } = "ILS";
        public string? PaymentTerms { get; set; }
        public string? Notes { get; set; }
        public string? InternalNotes { get; set; }
        public User? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<PurchaseOrderItem> Items { get; set; } = new();

        // Calculated
        public bool CanEdit => Status == PurchaseOrderStatus.Draft;
        public bool CanSend => Status == PurchaseOrderStatus.Draft && Items.Any();
        public bool CanConfirm => Status == PurchaseOrderStatus.Sent;
        public bool CanReceive => Status == PurchaseOrderStatus.Confirmed || Status == PurchaseOrderStatus.Shipped || Status == PurchaseOrderStatus.PartiallyReceived;
        public bool CanCancel => Status != PurchaseOrderStatus.Received && Status != PurchaseOrderStatus.Cancelled;
    }

    // ViewModel for receiving goods
    public class ReceiveGoodsViewModel
    {
        public int PurchaseOrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;

        [Display(Name = "תאריך קבלה")]
        [DataType(DataType.Date)]
        public DateTime ReceiveDate { get; set; } = DateTime.Now;

        [Display(Name = "הערות קבלה")]
        public string? Notes { get; set; }

        public List<ReceiveItemViewModel> Items { get; set; } = new();
    }

    public class ReceiveItemViewModel
    {
        public int ItemId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductSku { get; set; } = string.Empty;
        public string? UnitCode { get; set; }
        public decimal OrderedQuantity { get; set; }
        public decimal PreviouslyReceived { get; set; }
        public decimal RemainingQuantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal QuantityToReceive { get; set; }
    }

    // ViewModel for dashboard/summary
    public class PurchaseOrderDashboardViewModel
    {
        public int TotalOrders { get; set; }
        public int DraftOrders { get; set; }
        public int PendingOrders { get; set; }
        public int ReceivedOrders { get; set; }
        public decimal TotalPurchaseValue { get; set; }
        public decimal PendingValue { get; set; }
        public List<PurchaseOrderListViewModel> RecentOrders { get; set; } = new();
        public List<PurchaseOrderListViewModel> PendingDeliveries { get; set; } = new();
    }
}
