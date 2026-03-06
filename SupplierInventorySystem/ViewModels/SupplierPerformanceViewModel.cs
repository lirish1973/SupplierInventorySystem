namespace SupplierInventorySystem.ViewModels
{
    public class SupplierPerformanceDto
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string? SupplierCode { get; set; }
        public decimal? Rating { get; set; }

        // נתונים מחושבים מהזמנות רכש
        public int TotalOrders { get; set; }
        public int ReceivedOrders { get; set; }
        public decimal OnTimeRate { get; set; }         // אחוז אספקה בזמן
        public double AvgDelayDays { get; set; }        // עיכוב ממוצע בימים
        public decimal TotalPurchased { get; set; }     // סה"כ רכישות
        public decimal AvgOrderValue { get; set; }      // ערך הזמנה ממוצע

        // מטריקות ידניות (SupplierMetric)
        public decimal? ManualOnTimeRate { get; set; }
        public decimal? ManualDefectRate { get; set; }
        public DateTime? LastMetricDate { get; set; }

        // חישובים נוחים לתצוגה
        public string OnTimeRateClass => OnTimeRate >= 90 ? "success" : OnTimeRate >= 70 ? "warning" : ReceivedOrders > 0 ? "danger" : "secondary";
        public string OnTimeRateLabel => ReceivedOrders == 0 ? "אין נתונים" : $"{OnTimeRate}%";
        public string AvgDelayLabel => ReceivedOrders == 0 ? "-" : AvgDelayDays > 0 ? $"+{AvgDelayDays} ימים" : AvgDelayDays < 0 ? $"{AvgDelayDays} ימים (מוקדם)" : "בזמן";
    }
}
