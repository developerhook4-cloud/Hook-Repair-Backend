using FluentAssertions;
using HookRepair.Modules.Auth.Application.Security;
using HookRepair.Modules.Auth.Infrastructure.Security;
using HookRepair.Modules.Crm.Application.Suppliers;
using HookRepair.Modules.Inventory.Application.PurchaseOrders;
using HookRepair.Modules.Inventory.Core.Entities;
using HookRepair.Modules.Inventory.Infrastructure;
using HookRepair.Modules.Inventory.Infrastructure.PurchaseOrders;
using Microsoft.EntityFrameworkCore;

namespace HookRepair.Tests;

public sealed class PurchaseOrderServiceTests
{
    [Fact]
    public async Task Create_saves_order_as_pending_and_unpaid()
    {
        var ctx = await Setup();

        var result = await ctx.Service.CreateAsync(ctx.OrgId, ctx.UserId,
            new CreatePurchaseOrderRequest(ctx.SupplierId, [new(ctx.ProductA, 3)]));

        result.Status.Should().Be(PurchaseOrderStatus.Pending);
        result.PaymentStatus.Should().Be(PurchasePaymentStatus.Unpaid);
        result.SupplierId.Should().Be(ctx.SupplierId);
        result.CreatedByUserId.Should().Be(ctx.UserId);
        var stored = await ctx.Db.PurchaseOrders.Include(o => o.Lines).SingleAsync();
        stored.OrganizationId.Should().Be(ctx.OrgId);
        stored.Lines.Should().ContainSingle().Which.Quantity.Should().Be(3);
    }

    [Fact]
    public async Task Create_uses_product_cost_and_computes_total()
    {
        var ctx = await Setup();

        var result = await ctx.Service.CreateAsync(ctx.OrgId, ctx.UserId,
            new CreatePurchaseOrderRequest(ctx.SupplierId, [new(ctx.ProductA, 3), new(ctx.ProductB, 2)]));

        result.Lines.Single(l => l.ProductId == ctx.ProductA).UnitCost.Should().Be(10.15m);
        result.Total.Should().Be(3 * 10.15m + 2 * 99_999.99m);
    }

    [Fact]
    public async Task Create_does_not_change_stock()
    {
        var ctx = await Setup();

        await ctx.Service.CreateAsync(ctx.OrgId, ctx.UserId, new CreatePurchaseOrderRequest(ctx.SupplierId, [new(ctx.ProductA, 5)]));

        (await ctx.Db.Products.SingleAsync(p => p.Id == ctx.ProductA)).InStock.Should().Be(0);
        (await ctx.Db.InventoryMovements.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Create_rejects_unknown_or_inactive_supplier()
    {
        var ctx = await Setup();

        await FluentActions.Invoking(() => ctx.Service.CreateAsync(ctx.OrgId, ctx.UserId,
                new CreatePurchaseOrderRequest(Guid.NewGuid(), [new(ctx.ProductA, 1)])))
            .Should().ThrowAsync<ArgumentException>().WithMessage("*supplierId*");

        ctx.Suppliers.Active = false;
        await FluentActions.Invoking(() => ctx.Service.CreateAsync(ctx.OrgId, ctx.UserId,
                new CreatePurchaseOrderRequest(ctx.SupplierId, [new(ctx.ProductA, 1)])))
            .Should().ThrowAsync<ArgumentException>().WithMessage("*supplierId*");
    }

    [Fact]
    public async Task Create_rejects_inactive_or_foreign_product()
    {
        var ctx = await Setup();

        await FluentActions.Invoking(() => ctx.Service.CreateAsync(ctx.OrgId, ctx.UserId,
                new CreatePurchaseOrderRequest(ctx.SupplierId, [new(ctx.InactiveProduct, 1)])))
            .Should().ThrowAsync<ArgumentException>().WithMessage("*productId*");
        await FluentActions.Invoking(() => ctx.Service.CreateAsync(ctx.OrgId, ctx.UserId,
                new CreatePurchaseOrderRequest(ctx.SupplierId, [new(ctx.ForeignProduct, 1)])))
            .Should().ThrowAsync<ArgumentException>().WithMessage("*productId*");
    }

    [Fact]
    public async Task GetById_hides_other_tenant_order()
    {
        var ctx = await Setup();
        var foreign = new PurchaseOrder { OrganizationId = Guid.NewGuid(), SupplierId = Guid.NewGuid(), CreatedByUserId = Guid.NewGuid() };
        ctx.Db.PurchaseOrders.Add(foreign);
        await ctx.Db.SaveChangesAsync();

        (await ctx.Service.GetByIdAsync(ctx.OrgId, foreign.Id)).Should().BeNull();
        (await ctx.Service.GetAllAsync(ctx.OrgId)).Should().BeEmpty();
    }

    [Fact]
    public async Task Operations_reject_mismatched_organization()
    {
        var ctx = await Setup();

        await FluentActions.Invoking(() => ctx.Service.GetAllAsync(Guid.NewGuid()))
            .Should().ThrowAsync<TenantAccessException>();
    }

    private sealed record Ctx(PurchaseOrderService Service, InventoryDbContext Db, FakeSuppliers Suppliers,
        Guid OrgId, Guid UserId, Guid SupplierId, Guid ProductA, Guid ProductB, Guid InactiveProduct, Guid ForeignProduct);

    private static async Task<Ctx> Setup()
    {
        var orgId = Guid.NewGuid();
        var tenant = new TenantContext();
        tenant.Set(orgId);
        var db = new InventoryDbContext(
            new DbContextOptionsBuilder<InventoryDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tenant);
        Product P(Guid org, string name, decimal price, bool active = true) =>
            new() { Id = Guid.NewGuid(), OrganizationId = org, Name = name, Price = price, Active = active };
        var a = P(orgId, "A", 10.15m);
        var b = P(orgId, "B", 99_999.99m);
        var inactive = P(orgId, "I", 1m, active: false);
        var foreign = P(Guid.NewGuid(), "F", 1m);
        db.Products.AddRange(a, b, inactive, foreign);
        await db.SaveChangesAsync();
        var suppliers = new FakeSuppliers();
        return new Ctx(new PurchaseOrderService(db, tenant, suppliers), db, suppliers, orgId, Guid.NewGuid(),
            suppliers.Id, a.Id, b.Id, inactive.Id, foreign.Id);
    }

    private sealed class FakeSuppliers : ISupplierService
    {
        public Guid Id { get; } = Guid.NewGuid();
        public bool Active { get; set; } = true;

        public Task<SupplierResponse?> GetByIdAsync(Guid o, Guid id, CancellationToken c = default) =>
            Task.FromResult(id == Id ? new SupplierResponse(id, "S", "1", null, "s@x.com", Active) : null);
        public Task<IReadOnlyList<SupplierResponse>> GetAllAsync(Guid o, CancellationToken c = default) => throw new NotSupportedException();
        public Task<SupplierResponse> CreateAsync(Guid o, CreateSupplierRequest r, CancellationToken c = default) => throw new NotSupportedException();
        public Task<SupplierResponse?> UpdateAsync(Guid o, Guid id, UpdateSupplierRequest r, CancellationToken c = default) => throw new NotSupportedException();
        public Task<SupplierResponse?> DeactivateAsync(Guid o, Guid id, CancellationToken c = default) => throw new NotSupportedException();
    }
}
