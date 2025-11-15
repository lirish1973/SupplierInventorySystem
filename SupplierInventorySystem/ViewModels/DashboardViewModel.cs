using SupplierInventorySystem.Models;

namespace SupplierInventorySystem.ViewModels
{
    public class DashboardViewModel
    {
        // Suppliers Statistics
        public int TotalSuppliers { get; set; }
        public int ActiveSuppliers { get; set; }
        public int InactiveSuppliers { get; set; }
        public int BlockedSuppliers { get; set; }

        // Products Statistics
        public int TotalProducts { get; set; }
        public int ActiveProducts { get; set; }
        public int InactiveProducts { get; set; }
        public int ServiceProducts { get; set; }

        // Categories & Users Statistics
        public int TotalCategories { get; set; }
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }

        // Lists
        public List<LowStockProductDto> LowStockProducts { get; set; } = new();
        public List<TopSupplierDto> TopSuppliers { get; set; } = new();
        public List<Product> RecentProducts { get; set; } = new();
        public List<Supplier> RecentSuppliers { get; set; } = new();
        public List<CategoryStatDto> ProductsByCategory { get; set; } = new();
        public List<AlertDto> Alerts { get; set; } = new();
    }

    public class LowStockProductDto
    {
        public int Id { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal ReorderPoint { get; set; }
        public decimal ReorderQty { get; set; }
        public string Unit { get; set; } = string.Empty;
    }

    public class TopSupplierDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public decimal Rating { get; set; }
        public int ProductCount { get; set; }
    }

    public class CategoryStatDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public int ProductCount { get; set; }
    }

    public class AlertDto
    {
        public string Type { get; set; } = string.Empty; // success, info, warning, danger
        public string Icon { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
    }
}