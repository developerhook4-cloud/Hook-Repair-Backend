using HookRepair.Modules.Auth.Application.Security;
using HookRepair.Modules.Crm.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace HookRepair.Modules.Crm.Infrastructure
{
    public class CrmDbContext : DbContext
    {
        private readonly ITenantContext _tenantContext;

        public CrmDbContext(DbContextOptions<CrmDbContext> options, ITenantContext tenantContext) : base(options)
        {
            _tenantContext = tenantContext;
        }

        public DbSet<Customer> Customers { get; set; } = null!;
        public DbSet<Supplier> Suppliers { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasDefaultSchema("crm");

            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(item => item.Id);
                entity.Property(item => item.OrganizationId).IsRequired();

                entity.HasQueryFilter(item =>
                    _tenantContext.IsAuthenticated && item.OrganizationId == _tenantContext.OrganizationId);

                entity.Property(item => item.Name).HasMaxLength(200).IsRequired();
                entity.Property(item => item.Phone1).HasMaxLength(20).IsRequired();
                entity.Property(item => item.Phone2).HasMaxLength(20);
                entity.OwnsOne(item => item.Address);
                entity.Property(item => item.CustomerType)
                    .HasConversion(
                         v => v.ToString().ToLower(),
                         v => (CustomerType)Enum.Parse(typeof(CustomerType), v, true)
                    )
                    .IsRequired();

                entity.Property(item => item.AuthorizedPeople).HasColumnType("text[]");
                entity.Property(item => item.Rfc).HasMaxLength(13);

                entity.Property(item => item.Active).IsRequired();
            });

            modelBuilder.Entity<Supplier>(entity =>
            {
                entity.HasKey(item => item.Id);
                entity.Property(item => item.OrganizationId).IsRequired();

                entity.HasQueryFilter(item =>
                    _tenantContext.IsAuthenticated && item.OrganizationId == _tenantContext.OrganizationId);

                entity.Property(item => item.Name).HasMaxLength(200).IsRequired();
                entity.Property(item => item.Phone1).IsRequired();
                entity.Property(item => item.Email).IsRequired();

                entity.Property(item => item.Active).IsRequired();
            });
        }
    }
}
