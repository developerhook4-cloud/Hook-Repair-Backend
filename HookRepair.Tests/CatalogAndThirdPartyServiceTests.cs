using FluentAssertions;
using HookRepair.Modules.Auth.Application.Security;
using HookRepair.Modules.Auth.Infrastructure;
using HookRepair.Modules.Auth.Infrastructure.Security;
using HookRepair.Modules.Catalogs.Application.Catalogs;
using HookRepair.Modules.Catalogs.Core.Entities;
using HookRepair.Modules.Catalogs.Infrastructure;
using HookRepair.Modules.Catalogs.Infrastructure.Catalogs;
using HookRepair.Modules.Crm.Application.Clients;
using HookRepair.Modules.Crm.Application.Suppliers;
using HookRepair.Modules.Crm.Core.Entities;
using HookRepair.Modules.Crm.Infrastructure;
using HookRepair.Modules.Crm.Infrastructure.Clients;
using HookRepair.Modules.Crm.Infrastructure.Suppliers;
using Microsoft.EntityFrameworkCore;

namespace HookRepair.Tests;

public sealed class CatalogAndThirdPartyServiceTests
{
    [Fact]
    public async Task Category_get_all_is_tenant_scoped_and_sorted()
    {
        var tenant = NewTenant(out var organizationId);
        await using var db = CreateCatalogDbContext(tenant);
        db.Categories.AddRange(
            new Category { OrganizationId = organizationId, Name = "Zeta", Icon = "" },
            new Category { OrganizationId = organizationId, Name = "Alpha", Icon = "" },
            new Category { OrganizationId = Guid.NewGuid(), Name = "Other", Icon = "" });
        await db.SaveChangesAsync();

        var result = await new CategoryService(db, tenant).GetAllAsync(organizationId);

        result.Select(item => item.Name).Should().Equal("Alpha", "Zeta");
    }

    [Fact]
    public async Task Brand_create_trims_name_and_activates_record()
    {
        var tenant = NewTenant(out var organizationId);
        await using var db = CreateCatalogDbContext(tenant);

        var result = await new BrandService(db, tenant).CreateAsync(
            organizationId,
            new CreateBrandRequest("  Apple  "));

        result.Name.Should().Be("Apple");
        result.Active.Should().BeTrue();
        (await db.Brands.SingleAsync()).OrganizationId.Should().Be(organizationId);
    }

    [Fact]
    public async Task Model_create_rejects_brand_from_another_tenant()
    {
        var tenant = NewTenant(out var organizationId);
        await using var db = CreateCatalogDbContext(tenant);
        var brand = new Brand { OrganizationId = Guid.NewGuid(), Name = "Other" };
        db.Brands.Add(brand);
        await db.SaveChangesAsync();

        var action = () => new ModelService(db, tenant).CreateAsync(
            organizationId,
            new CreateModelRequest("iPhone", brand.Id));

        await action.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Category_deactivate_keeps_row_and_marks_inactive()
    {
        var tenant = NewTenant(out var organizationId);
        await using var db = CreateCatalogDbContext(tenant);
        var category = new Category { OrganizationId = organizationId, Name = "Phones" };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var result = await new CategoryService(db, tenant).DeactivateAsync(organizationId, category.Id);

        result!.Active.Should().BeFalse();
        (await db.Categories.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Client_create_uses_crm_customer_and_preserves_distributor_type()
    {
        var tenant = NewTenant(out var organizationId);
        await using var db = CreateCrmDbContext(tenant);

        var result = await new CustomerService(db, tenant).CreateAsync(
            organizationId,
            new CreateClientRequest
            {
                Name = "  Ana  ",
                CustomerType = CustomerType.Distributor,
                Phone1 = " 555-0100 ",
                Email = " ana@example.com ",
                Address = new AddressRequest { Address = "  Main 1 ", City = "  Phoenix " }
            });

        result.Name.Should().Be("Ana");
        result.CustomerType.Should().Be(CustomerType.Distributor);
        result.Phone1.Should().Be("555-0100");
        result.Email.Should().Be("ana@example.com");
        result.Address!.Address.Should().Be("Main 1");
        result.Address.City.Should().Be("Phoenix");
        result.Active.Should().BeTrue();
        (await db.Customers.SingleAsync()).CustomerType.Should().Be(CustomerType.Distributor);
    }

    [Fact]
    public async Task Supplier_cross_tenant_access_is_rejected_by_crm_context_filter()
    {
        var tenant = NewTenant(out var organizationId);
        await using var db = CreateCrmDbContext(tenant);
        var supplier = new Supplier { OrganizationId = Guid.NewGuid(), Name = "Other", Phone1 = "555", Email = "other@example.com" };
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync();

        var result = await new SupplierService(db, tenant).GetByIdAsync(organizationId, supplier.Id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Client_deactivate_keeps_crm_customer_row()
    {
        var tenant = NewTenant(out var organizationId);
        await using var db = CreateCrmDbContext(tenant);
        var client = new Customer { OrganizationId = organizationId, Name = "Ana", Phone1 = "555" };
        db.Customers.Add(client);
        await db.SaveChangesAsync();

        var result = await new CustomerService(db, tenant).DeactivateAsync(organizationId, client.Id);

        result!.Active.Should().BeFalse();
        (await db.Customers.CountAsync()).Should().Be(1);
    }

    [Fact]
    public void Domain_entities_are_registered_in_catalogs_and_crm_not_auth()
    {
        var tenant = NewTenant(out _);
        using var auth = CreateAuthDbContext(tenant);
        using var catalogs = CreateCatalogDbContext(tenant);
        using var crm = CreateCrmDbContext(tenant);

        auth.Model.GetEntityTypes().Select(item => item.ClrType)
            .Should().NotContain(typeof(Category))
            .And.NotContain(typeof(Brand))
            .And.NotContain(typeof(Model))
            .And.NotContain(typeof(Customer))
            .And.NotContain(typeof(Supplier));
        catalogs.Model.GetEntityTypes().Select(item => item.ClrType)
            .Should().Contain(typeof(Category))
            .And.Contain(typeof(Brand))
            .And.Contain(typeof(Model));
        crm.Model.GetEntityTypes().Select(item => item.ClrType)
            .Should().Contain(typeof(Customer))
            .And.Contain(typeof(Supplier));

        catalogs.Model.FindEntityType(typeof(Model))!.GetForeignKeys()
            .Single(item => item.Properties.Single().Name == nameof(Model.BrandId))
            .DeleteBehavior.Should().Be(DeleteBehavior.Restrict);
    }

    private static TenantContext NewTenant(out Guid organizationId)
    {
        organizationId = Guid.NewGuid();
        var tenant = new TenantContext();
        tenant.Set(organizationId);
        return tenant;
    }

    private static CatalogDbContext CreateCatalogDbContext(ITenantContext tenant) =>
        new(
            new DbContextOptionsBuilder<CatalogDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            tenant);

    private static AuthDbContext CreateAuthDbContext(ITenantContext tenant) =>
        new(
            new DbContextOptionsBuilder<AuthDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            tenant);

    private static CrmDbContext CreateCrmDbContext(ITenantContext tenant) =>
        new(
            new DbContextOptionsBuilder<CrmDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            tenant);
}
