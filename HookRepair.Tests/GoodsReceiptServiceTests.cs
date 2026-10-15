using FluentAssertions;
using HookRepair.Modules.Auth.Application.Security;
using HookRepair.Modules.Auth.Infrastructure.Security;
using HookRepair.Modules.Inventory.Application.GoodsReceipts;
using HookRepair.Modules.Inventory.Core.Entities;
using HookRepair.Modules.Inventory.Infrastructure;
using HookRepair.Modules.Inventory.Infrastructure.GoodsReceipts;
using Microsoft.EntityFrameworkCore;

namespace HookRepair.Tests;

public sealed class GoodsReceiptServiceTests
{
    [Fact]
    public async Task Create_without_lines_receives_everything_pending()
    {
        var ctx = await Setup();

        var result = await ctx.Service.CreateAsync(ctx.OrgId, ctx.UserId, new CreateGoodsReceiptRequest(ctx.OrderId, null, null));

        result.PurchaseOrderStatus.Should().Be(PurchaseOrderStatus.Received);
        result.ReceivedByUserId.Should().Be(ctx.UserId);
        result.Lines.Should().HaveCount(2);
        result.Lines.Single(l => l.ProductId == ctx.ProductA).ReceivedQuantity.Should().Be(5);
        (await Stock(ctx, ctx.ProductA)).Should().Be(7);
        (await Stock(ctx, ctx.ProductB)).Should().Be(3);
    }

    [Fact]
    public async Task Create_with_less_quantity_marks_order_as_partially_received()
    {
        var ctx = await Setup();

        var result = await ctx.Service.CreateAsync(ctx.OrgId, ctx.UserId,
            new CreateGoodsReceiptRequest(ctx.OrderId, [new(ctx.LineA, 2), new(ctx.LineB, 0)], " Faltó mercancía "));

        result.PurchaseOrderStatus.Should().Be(PurchaseOrderStatus.ReceivedPartially);
        result.Notes.Should().Be("Faltó mercancía");
        result.Lines.Should().ContainSingle().Which.ReceivedQuantity.Should().Be(2);
        (await Stock(ctx, ctx.ProductA)).Should().Be(4);
        (await Stock(ctx, ctx.ProductB)).Should().Be(0);
        var movement = await ctx.Db.InventoryMovements.SingleAsync();
        movement.Type.Should().Be(InventoryMovementType.Entry);
        movement.Quantity.Should().Be(2);
        movement.PreviousStock.Should().Be(2);
        movement.CurrentStock.Should().Be(4);
    }

    [Fact]
    public async Task Second_receipt_completes_pending_quantities()
    {
        var ctx = await Setup();
        await ctx.Service.CreateAsync(ctx.OrgId, ctx.UserId, new CreateGoodsReceiptRequest(ctx.OrderId, [new(ctx.LineA, 2)], null));

        var result = await ctx.Service.CreateAsync(ctx.OrgId, ctx.UserId, new CreateGoodsReceiptRequest(ctx.OrderId, null, null));

        result.PurchaseOrderStatus.Should().Be(PurchaseOrderStatus.Received);
        result.Lines.Should().ContainSingle().Which.ReceivedQuantity.Should().Be(3);
        (await Stock(ctx, ctx.ProductA)).Should().Be(7);
        (await Stock(ctx, ctx.ProductB)).Should().Be(3);
        (await ctx.Service.GetAllAsync(ctx.OrgId, ctx.OrderId)).Should().HaveCount(2);
    }

    [Fact]
    public async Task Create_rejects_quantity_above_pending()
    {
        var ctx = await Setup();

        await FluentActions.Invoking(() => ctx.Service.CreateAsync(ctx.OrgId, ctx.UserId,
                new CreateGoodsReceiptRequest(ctx.OrderId, [new(ctx.LineA, 6)], null)))
            .Should().ThrowAsync<ArgumentException>().WithMessage("*pending*");
        (await Stock(ctx, ctx.ProductA)).Should().Be(2);
    }

    [Fact]
    public async Task Create_rejects_receipt_without_units()
    {
        var ctx = await Setup();

        await FluentActions.Invoking(() => ctx.Service.CreateAsync(ctx.OrgId, ctx.UserId,
                new CreateGoodsReceiptRequest(ctx.OrderId, [new(ctx.LineA, 0), new(ctx.LineB, 0)], null)))
            .Should().ThrowAsync<ArgumentException>().WithMessage("*at least one unit*");
    }

    [Theory]
    [InlineData(PurchaseOrderStatus.Received)]
    [InlineData(PurchaseOrderStatus.Canceled)]
    [InlineData(PurchaseOrderStatus.InRma)]
    public async Task Create_rejects_orders_that_cannot_receive(PurchaseOrderStatus status)
    {
        var ctx = await Setup();
        (await ctx.Db.PurchaseOrders.SingleAsync()).Status = status;
        await ctx.Db.SaveChangesAsync();

        await FluentActions.Invoking(() => ctx.Service.CreateAsync(ctx.OrgId, ctx.UserId,
                new CreateGoodsReceiptRequest(ctx.OrderId, null, null)))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Create_rejects_line_from_another_order()
    {
        var ctx = await Setup();

        await FluentActions.Invoking(() => ctx.Service.CreateAsync(ctx.OrgId, ctx.UserId,
                new CreateGoodsReceiptRequest(ctx.OrderId, [new(Guid.NewGuid(), 1)], null)))
            .Should().ThrowAsync<ArgumentException>().WithMessage("*purchaseOrderLineId*");
    }

    [Fact]
    public async Task Create_rejects_serialized_products()
    {
        var ctx = await Setup();
        (await ctx.Db.Products.SingleAsync(p => p.Id == ctx.ProductB)).IsSerialized = true;
        await ctx.Db.SaveChangesAsync();

        await FluentActions.Invoking(() => ctx.Service.CreateAsync(ctx.OrgId, ctx.UserId,
                new CreateGoodsReceiptRequest(ctx.OrderId, null, null)))
            .Should().ThrowAsync<ArgumentException>().WithMessage("*serialized*");
    }

    [Fact]
    public async Task Create_rejects_unknown_or_foreign_order()
    {
        var ctx = await Setup();
        var foreign = new PurchaseOrder { OrganizationId = Guid.NewGuid(), SupplierId = Guid.NewGuid(), CreatedByUserId = Guid.NewGuid() };
        ctx.Db.PurchaseOrders.Add(foreign);
        await ctx.Db.SaveChangesAsync();

        await FluentActions.Invoking(() => ctx.Service.CreateAsync(ctx.OrgId, ctx.UserId,
                new CreateGoodsReceiptRequest(Guid.NewGuid(), null, null)))
            .Should().ThrowAsync<KeyNotFoundException>();
        await FluentActions.Invoking(() => ctx.Service.CreateAsync(ctx.OrgId, ctx.UserId,
                new CreateGoodsReceiptRequest(foreign.Id, null, null)))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Operations_reject_mismatched_organization()
    {
        var ctx = await Setup();

        await FluentActions.Invoking(() => ctx.Service.GetAllAsync(Guid.NewGuid()))
            .Should().ThrowAsync<TenantAccessException>();
        await FluentActions.Invoking(() => ctx.Service.CreateAsync(Guid.NewGuid(), ctx.UserId,
                new CreateGoodsReceiptRequest(ctx.OrderId, null, null)))
            .Should().ThrowAsync<TenantAccessException>();
    }

    private static async Task<int> Stock(Ctx ctx, Guid productId) =>
        (await ctx.Db.Products.AsNoTracking().SingleAsync(p => p.Id == productId)).InStock;

    private sealed record Ctx(GoodsReceiptService Service, InventoryDbContext Db,
        Guid OrgId, Guid UserId, Guid OrderId, Guid LineA, Guid LineB, Guid ProductA, Guid ProductB);

    private static async Task<Ctx> Setup()
    {
        var orgId = Guid.NewGuid();
        var tenant = new TenantContext();
        tenant.Set(orgId);
        var db = new InventoryDbContext(
            new DbContextOptionsBuilder<InventoryDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tenant);
        var a = new Product { Id = Guid.NewGuid(), OrganizationId = orgId, Name = "A", Price = 10m, InStock = 2, Active = true };
        var b = new Product { Id = Guid.NewGuid(), OrganizationId = orgId, Name = "B", Price = 20m, Active = true };
        var order = new PurchaseOrder { OrganizationId = orgId, SupplierId = Guid.NewGuid(), CreatedByUserId = Guid.NewGuid() };
        var lineA = new PurchaseOrderLine { OrganizationId = orgId, PurchaseOrderId = order.Id, ProductId = a.Id, Quantity = 5, UnitCost = 10m };
        var lineB = new PurchaseOrderLine { OrganizationId = orgId, PurchaseOrderId = order.Id, ProductId = b.Id, Quantity = 3, UnitCost = 20m };
        order.Lines.Add(lineA);
        order.Lines.Add(lineB);
        db.Products.AddRange(a, b);
        db.PurchaseOrders.Add(order);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return new Ctx(new GoodsReceiptService(db, tenant), db, orgId, Guid.NewGuid(), order.Id, lineA.Id, lineB.Id, a.Id, b.Id);
    }
}
