using Microsoft.EntityFrameworkCore;
using SupplierInventorySystem.Models;

namespace SupplierInventorySystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets for all tables
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<SupplierContact> SupplierContacts { get; set; }
        public DbSet<SupplierAddress> SupplierAddresses { get; set; }
        public DbSet<SupplierMetric> SupplierMetrics { get; set; }
        public DbSet<ProductCategory> ProductCategories { get; set; }
        public DbSet<Unit> Units { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<SupplierProduct> SupplierProducts { get; set; }
        public DbSet<ProductPriceHistory> ProductPriceHistories { get; set; }
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
        public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<StockAdjustmentLog> StockAdjustmentLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Composite key for RolePermission
            modelBuilder.Entity<RolePermission>()
                .HasKey(rp => new { rp.RoleId, rp.PermissionId });

            // Configure relationships and indexes as needed

            // ProductImage configuration
            modelBuilder.Entity<ProductImage>()
                .HasOne(pi => pi.Product)
                .WithMany(p => p.ProductImages)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductImage>()
                .HasIndex(pi => new { pi.ProductId, pi.DisplayOrder });
        }
    }
}