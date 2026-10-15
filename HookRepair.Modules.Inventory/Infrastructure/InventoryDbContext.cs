using HookRepair.Modules.Auth.Application.Security;
using HookRepair.Modules.Auth.Core.Entities;
using HookRepair.Modules.Catalogs.Core.Entities;
using HookRepair.Modules.Crm.Core.Entities;
using HookRepair.Modules.Inventory.Core.Entities;
using HookRepair.Shared.Core.ValueObjects;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HookRepair.Modules.Inventory.Infrastructure
{
    public class InventoryDbContext : DbContext
    {
        private readonly ITenantContext _tenantContext;

        public InventoryDbContext(DbContextOptions<InventoryDbContext> options, ITenantContext tenantContext)
            : base(options)
        {
            _tenantContext = tenantContext;
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<ProductModel> ProductModels { get; set; }
        public DbSet<ProductSerial> ProductSerials { get; set; }
        public DbSet<InventoryMovement> InventoryMovements { get; set; }
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
        public DbSet<PurchaseOrderLine> PurchaseOrderLines { get; set; }
        public DbSet<GoodsReceipt> GoodsReceipts { get; set; }
        public DbSet<GoodsReceiptLine> GoodsReceiptLines { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasDefaultSchema("inventory");
            modelBuilder.Ignore<FullAddress>();

            modelBuilder.Entity<Product>((entity) =>
            {
                entity.HasKey(item => item.Id);
                entity.Property(item => item.ProductNumber)
                    .ValueGeneratedOnAdd()
                    .UseIdentityColumn();

                entity.Property(item => item.OrganizationId).IsRequired();

                entity.HasQueryFilter(item =>
                    _tenantContext.IsAuthenticated && item.OrganizationId == _tenantContext.OrganizationId);

                entity.Property(item => item.Name).HasMaxLength(250).IsRequired();
                entity.Property(item => item.UPC).HasMaxLength(12).IsRequired();
                entity.HasIndex(item => new { item.UPC, item.OrganizationId }).IsUnique();
                entity.Property(item => item.UPC)
                    .HasComputedColumnSql("'4' || (CASE WHEN \"is_serialized\" THEN '1' ELSE '0' END) || LPAD(\"product_number\"::text, 10, '0')", stored: true);
                entity.HasOne(item => item.Category)
                       .WithMany()
                       .HasForeignKey(item => item.CategoryId)
                       .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(item => item.Brand)
                       .WithMany()
                       .HasForeignKey(item => item.BrandId)
                       .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(item => item.Supplier)
                       .WithMany()
                       .HasForeignKey(item => item.SupplierId)
                       .OnDelete(DeleteBehavior.Restrict);
                entity.Property(item => item.InStock).IsRequired();
                entity.Property(item => item.StockWarning).IsRequired();
                entity.Property(item => item.MinimumReorderPoint).IsRequired();
                entity.Property(item => item.SupplierId);
                entity.Property(item => item.Price).HasPrecision(18, 2).IsRequired();
                entity.Property(item => item.GeneralSalePrice).HasPrecision(18, 2).IsRequired();
                entity.Property(item => item.DistributorSalePrice).HasPrecision(18, 2).IsRequired();
                entity.Property(item => item.IsSerialized).IsRequired();
                entity.Property(item => item.Active).IsRequired();
            });

            modelBuilder.Entity<ProductModel>((entity) =>
            {
                entity.HasKey(item => item.Id);
                entity.Property(item => item.OrganizationId).IsRequired();

                entity.HasQueryFilter(item =>
                    _tenantContext.IsAuthenticated && item.OrganizationId == _tenantContext.OrganizationId);

                entity.HasOne(item => item.Product)
                       .WithMany(p => p.Models)
                       .HasForeignKey(item => item.ProductId)
                       .OnDelete(DeleteBehavior.Restrict);
                entity.Property(item => item.ModelId).IsRequired();
            });

            modelBuilder.Entity<ProductSerial>((entity) =>
            {
                entity.HasKey(item => item.Id);
                entity.Property(item => item.OrganizationId).IsRequired();
                entity.HasQueryFilter(item =>
                    _tenantContext.IsAuthenticated && item.OrganizationId == _tenantContext.OrganizationId);

                entity.HasOne(item => item.Product)
                      .WithMany(p => p.Serials)
                      .HasForeignKey(item => item.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.Property(item => item.IMEI).IsRequired();
                entity.HasIndex(item => new { item.IMEI, item.OrganizationId }).IsUnique();
                entity.Property(item => item.Status).HasMaxLength(50).IsRequired();
            });

            modelBuilder.Entity<InventoryMovement>((entity) =>
            {
                entity.HasKey(item => item.Id);

                 entity.Property(item => item.OrganizationId).IsRequired();

                entity.HasQueryFilter(item =>
                 _tenantContext.IsAuthenticated &&
                 item.OrganizationId == _tenantContext.OrganizationId);

                entity.HasOne(item => item.Product)
                        .WithMany(product => product.Movements)
                        .HasForeignKey(item => item.ProductId)
                        .OnDelete(DeleteBehavior.Restrict);

                entity.Property(item => item.Type)
                        .HasConversion<string>()
                        .HasMaxLength(20)
                        .IsRequired();

                entity.Property(item => item.Quantity).IsRequired();
                entity.Property(item => item.PreviousStock).IsRequired();
                entity.Property(item => item.CurrentStock).IsRequired();
                entity.Property(item => item.Reason).HasMaxLength(500);
                entity.Property(item => item.CreatedAtUtc).IsRequired();

                entity.HasIndex(item => new
                {
                    item.OrganizationId,
                    item.ProductId,
                    item.CreatedAtUtc
                });
            });

            modelBuilder.Entity<PurchaseOrder>(entity =>
            {
                entity.HasKey(item => item.Id);
                entity.Property(item => item.OrganizationId).IsRequired();
                entity.HasQueryFilter(item =>
                    _tenantContext.IsAuthenticated && item.OrganizationId == _tenantContext.OrganizationId);
                entity.Property(item => item.PurchaseOrderNumber)
                    .ValueGeneratedOnAdd()
                    .UseIdentityColumn();
                entity.Property(item => item.SupplierId).IsRequired();
                entity.HasOne(item => item.Supplier)
                      .WithMany()
                      .HasForeignKey(item => item.SupplierId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.Property(item => item.CreatedByUserId).IsRequired();
                entity.HasOne(item => item.User)
                      .WithMany()
                      .HasForeignKey(item => item.CreatedByUserId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.Property(item => item.CreatedAtUtc).IsRequired();
                entity.Property(item => item.Status)
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .IsRequired();
                entity.Property(item => item.PaymentStatus)
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .IsRequired();
                entity.Property(item => item.Total).HasPrecision(18, 2).IsRequired();
                entity.HasIndex(item => new { item.OrganizationId, item.CreatedAtUtc });
            });

            modelBuilder.Entity<PurchaseOrderLine>(entity =>
            {
                entity.HasKey(item => item.Id);
                entity.Property(item => item.OrganizationId).IsRequired();
                entity.HasQueryFilter(item =>
                    _tenantContext.IsAuthenticated && item.OrganizationId == _tenantContext.OrganizationId);
                entity.HasOne(item => item.PurchaseOrder)
                    .WithMany(order => order.Lines)
                    .HasForeignKey(item => item.PurchaseOrderId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(item => item.Product)
                    .WithMany()
                    .HasForeignKey(item => item.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.Property(item => item.Quantity).IsRequired();
                entity.Property(item => item.UnitCost).HasPrecision(18, 2).IsRequired();
                entity.HasIndex(item => new { item.PurchaseOrderId, item.ProductId }).IsUnique();
                entity.ToTable(table => table.HasCheckConstraint("ck_purchase_order_lines_quantity", "quantity > 0"));
            });

            modelBuilder.Entity<GoodsReceipt>(entity =>
            {
                entity.HasKey(item => item.Id);
                entity.Property(item => item.OrganizationId).IsRequired();
                entity.HasQueryFilter(item =>
                    _tenantContext.IsAuthenticated && item.OrganizationId == _tenantContext.OrganizationId);
                entity.Property(item => item.GoodsReceiptNumber)
                    .ValueGeneratedOnAdd()
                    .UseIdentityColumn();
                entity.HasOne(item => item.PurchaseOrder)
                    .WithMany()
                    .HasForeignKey(item => item.PurchaseOrderId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.Property(item => item.ReceivedByUserId).IsRequired();
                entity.Property(item => item.CreatedAtUtc).IsRequired();
                entity.Property(item => item.Notes).HasMaxLength(500);
                entity.HasIndex(item => new { item.OrganizationId, item.CreatedAtUtc });
            });

            modelBuilder.Entity<GoodsReceiptLine>(entity =>
            {
                entity.HasKey(item => item.Id);
                entity.Property(item => item.OrganizationId).IsRequired();
                entity.HasQueryFilter(item =>
                    _tenantContext.IsAuthenticated && item.OrganizationId == _tenantContext.OrganizationId);
                entity.HasOne(item => item.GoodsReceipt)
                    .WithMany(receipt => receipt.Lines)
                    .HasForeignKey(item => item.GoodsReceiptId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(item => item.PurchaseOrderLine)
                    .WithMany()
                    .HasForeignKey(item => item.PurchaseOrderLineId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(item => item.Product)
                    .WithMany()
                    .HasForeignKey(item => item.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.Property(item => item.OrderedQuantity).IsRequired();
                entity.Property(item => item.ReceivedQuantity).IsRequired();
                entity.HasIndex(item => new { item.GoodsReceiptId, item.PurchaseOrderLineId }).IsUnique();
                entity.ToTable(table => table.HasCheckConstraint("ck_goods_receipt_lines_received_quantity", "received_quantity >= 0"));
            });

            modelBuilder.Entity<Category>().ToTable("categories", "catalogs", t => t.ExcludeFromMigrations());
            modelBuilder.Entity<Brand>().ToTable("brands", "catalogs", t => t.ExcludeFromMigrations());
            modelBuilder.Entity<Model>().ToTable("models", "catalogs", t => t.ExcludeFromMigrations());
            modelBuilder.Entity<Supplier>().ToTable("suppliers", "crm", t => t.ExcludeFromMigrations());
            modelBuilder.Entity<User>().ToTable("users", "public", t => t.ExcludeFromMigrations());
        }
    }
}
