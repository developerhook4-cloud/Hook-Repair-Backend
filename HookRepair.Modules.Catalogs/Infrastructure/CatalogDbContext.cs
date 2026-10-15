using HookRepair.Modules.Auth.Application.Security;
using HookRepair.Modules.Catalogs.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace HookRepair.Modules.Catalogs.Infrastructure
{
    public sealed class CatalogDbContext : DbContext
    {
        private readonly ITenantContext _tenantContext;

        public CatalogDbContext(
            DbContextOptions<CatalogDbContext> options,
            ITenantContext tenantContext) : base(options)
        {
            _tenantContext = tenantContext;
        }

        public DbSet<Service> Services { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Brand> Brands { get; set; } = null!;
        public DbSet<Model> Models { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasDefaultSchema("catalogs");

            modelBuilder.Entity<Service>(entity =>
            {
                entity.HasKey(item => item.Id);
                entity.Property(item => item.OrganizationId).IsRequired();

                entity.HasQueryFilter(item =>
                    _tenantContext.IsAuthenticated && item.OrganizationId == _tenantContext.OrganizationId);

                entity.Property(item => item.Name).HasMaxLength(200).IsRequired();
                entity.Property(item => item.Description).HasMaxLength(2000);
                entity.Property(item => item.RetailPrice).HasPrecision(18, 2).IsRequired();
                entity.Property(item => item.PublicPrice).HasPrecision(18, 2).IsRequired();
                entity.Property(item => item.DistributorPrice).HasPrecision(18, 2).IsRequired();

                entity.HasOne(s => s.Category)
                      .WithMany()
                      .HasForeignKey(s => s.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(s => s.Brand)
                      .WithMany()
                      .HasForeignKey(s => s.BrandId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(s => s.Model)
                      .WithMany()
                      .HasForeignKey(s => s.ModelId)
                      .IsRequired(false)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(item => item.Id);
                entity.Property(item => item.OrganizationId).IsRequired();
                entity.Property(item => item.Name).HasMaxLength(150).IsRequired();
                entity.Property(item => item.Icon).IsRequired();
                entity.Property(item => item.Active).IsRequired();
                entity.HasIndex(item => new { item.OrganizationId, item.Name }).IsUnique();
                entity.HasQueryFilter(item =>
                    _tenantContext.IsAuthenticated && item.OrganizationId == _tenantContext.OrganizationId);
            });

            modelBuilder.Entity<Brand>(entity =>
            {
                entity.HasKey(item => item.Id);
                entity.Property(item => item.OrganizationId).IsRequired();
                entity.Property(item => item.Name).HasMaxLength(150).IsRequired();
                entity.Property(item => item.Active).IsRequired();
                entity.HasIndex(item => new { item.OrganizationId, item.Name }).IsUnique();
                entity.HasQueryFilter(item =>
                    _tenantContext.IsAuthenticated && item.OrganizationId == _tenantContext.OrganizationId);
            });

            modelBuilder.Entity<Model>(entity =>
            {
                entity.HasKey(item => item.Id);
                entity.Property(item => item.OrganizationId).IsRequired();
                entity.Property(item => item.BrandId).IsRequired();
                entity.Property(item => item.Name).HasMaxLength(150).IsRequired();
                entity.Property(item => item.Active).IsRequired();
                entity.HasIndex(item => new { item.OrganizationId, item.BrandId, item.Name }).IsUnique();
                entity.HasQueryFilter(item =>
                    _tenantContext.IsAuthenticated && item.OrganizationId == _tenantContext.OrganizationId);
                entity.HasOne(item => item.Brand)
                    .WithMany(item => item.Models)
                    .HasForeignKey(item => item.BrandId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

    }
}
