using HookRepair.Modules.Auth.Application.Security;
using HookRepair.Modules.Auth.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace HookRepair.Modules.Auth.Infrastructure;

public sealed class AuthDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public AuthDbContext(DbContextOptions<AuthDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public DbSet<Organization> Organizations { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.OrganizationId).IsRequired();
            entity.HasQueryFilter(item =>
                _tenantContext.IsAuthenticated && item.OrganizationId == _tenantContext.OrganizationId);
            entity.Property(item => item.RoleId).IsRequired();
            entity.Property(item => item.Name).HasMaxLength(150).IsRequired();
            entity.Property(item => item.Email).IsRequired();
            entity.HasIndex(item => item.Email).IsUnique();
            entity.Property(item => item.PasswordHash).HasColumnName("password_hash").IsRequired();
            entity.Property(item => item.IsActive).IsRequired();
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.OrganizationId).IsRequired();
            entity.Property(item => item.Active).IsRequired();
            entity.HasQueryFilter(item =>
                _tenantContext.IsAuthenticated && item.OrganizationId == _tenantContext.OrganizationId);
            entity.Property(item => item.Name).IsRequired();
            entity.Property(item => item.Permissions).HasColumnType("text[]").IsRequired();
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.UserId).IsRequired();
            entity.Property(item => item.TokenHash).HasColumnName("token_hash").IsRequired();
            entity.HasIndex(item => item.TokenHash).IsUnique();
            entity.Property(item => item.ExpiresAt).IsRequired();
            entity.Property(item => item.CreatedAt).IsRequired();
            entity.Property(item => item.RevokedAt).IsRequired(false);
            entity.HasQueryFilter(item =>
                _tenantContext.IsAuthenticated && item.User.OrganizationId == _tenantContext.OrganizationId);
        });

        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Name).HasMaxLength(200).IsRequired();
            entity.Property(item => item.Active).IsRequired();
            entity.Property(item => item.Email).HasMaxLength(200).IsRequired();
            entity.OwnsOne(item => item.Address);
        });
    }
}
