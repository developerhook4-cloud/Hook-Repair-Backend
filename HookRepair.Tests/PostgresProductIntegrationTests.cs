using FluentAssertions;
using HookRepair.Modules.Auth.Infrastructure.Security;
using HookRepair.Modules.Inventory.Core.Entities;
using HookRepair.Modules.Inventory.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HookRepair.Tests;

public sealed class PostgresProductIntegrationTests
{
    [Fact]
    public async Task Product_upc_is_generated_by_database_and_supplier_is_optional()
    {
        var connection = Environment.GetEnvironmentVariable("HOOKREPAIR_TEST_CONNECTION");
        Assert.False(string.IsNullOrWhiteSpace(connection), "HOOKREPAIR_TEST_CONNECTION is required.");
        var orgId = Guid.NewGuid();
        var tenant = new TenantContext();
        tenant.Set(orgId);
        await using var db = new InventoryDbContext(
            new DbContextOptionsBuilder<InventoryDbContext>().UseNpgsql(connection).UseSnakeCaseNamingConvention().Options,
            tenant);

        var product = new Product { Id = Guid.NewGuid(), OrganizationId = orgId, Name = "UPC test", IsSerialized = true, Active = true };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        await db.Entry(product).ReloadAsync();

        product.UPC.Should().MatchRegex("^41[0-9]{10}$");
        product.SupplierId.Should().BeNull();
    }
}
