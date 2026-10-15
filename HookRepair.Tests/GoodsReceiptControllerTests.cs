using System.Reflection;
using System.Security.Claims;
using FluentAssertions;
using HookRepair.Api.Controllers;
using HookRepair.Modules.Auth.Core.Constants;
using HookRepair.Modules.Auth.Infrastructure.Security;
using HookRepair.Modules.Inventory.Application.GoodsReceipts;
using HookRepair.Modules.Inventory.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HookRepair.Tests;

public sealed class GoodsReceiptControllerTests
{
    [Fact]
    public void Routes_and_policies_match_contract()
    {
        typeof(GoodsReceiptController).GetCustomAttribute<RouteAttribute>()!.Template
            .Should().Be("api/{orgId:guid}/goods-receipts");
        Policy(nameof(GoodsReceiptController.GetAll)).Should().Be(SystemPermissions.ViewInventory);
        Policy(nameof(GoodsReceiptController.GetById)).Should().Be(SystemPermissions.ViewInventory);
        Policy(nameof(GoodsReceiptController.Create)).Should().Be(SystemPermissions.UpdateInventory);
    }

    [Fact]
    public async Task Create_uses_authenticated_user_as_receiver()
    {
        var userId = Guid.NewGuid();
        var (controller, orgId, stub) = Build(userId);

        var result = await controller.Create(orgId, new CreateGoodsReceiptRequest(Guid.NewGuid(), null, null), CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>().Which.Value.Should().BeOfType<GoodsReceiptMutationResponse>();
        stub.LastReceivedBy.Should().Be(userId);
    }

    [Fact]
    public async Task Create_without_user_claim_is_unauthorized()
    {
        var (controller, orgId, _) = Build(null);

        var result = await controller.Create(orgId, new CreateGoodsReceiptRequest(Guid.NewGuid(), null, null), CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Create_rejects_missing_purchase_order()
    {
        var (controller, orgId, _) = Build(Guid.NewGuid());

        var result = await controller.Create(orgId, new CreateGoodsReceiptRequest(null, null, null), CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(400);
    }

    [Theory]
    [InlineData("argument", 400)]
    [InlineData("notfound", 404)]
    [InlineData("conflict", 409)]
    public async Task Create_maps_service_errors(string error, int status)
    {
        var (controller, orgId, stub) = Build(Guid.NewGuid());
        stub.Error = error;

        var result = await controller.Create(orgId, new CreateGoodsReceiptRequest(Guid.NewGuid(), null, null), CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(status);
    }

    [Fact]
    public async Task GetAll_rejects_other_organization()
    {
        var (controller, _, _) = Build(Guid.NewGuid());

        var result = await controller.GetAll(Guid.NewGuid(), null, CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task GetById_missing_returns_404()
    {
        var (controller, orgId, _) = Build(Guid.NewGuid());

        var result = await controller.GetById(orgId, Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(404);
    }

    private static string? Policy(string method) =>
        typeof(GoodsReceiptController).GetMethod(method)!.GetCustomAttribute<AuthorizeAttribute>()!.Policy;

    private static (GoodsReceiptController, Guid, StubService) Build(Guid? userId)
    {
        var orgId = Guid.NewGuid();
        var tenant = new TenantContext();
        tenant.Set(orgId);
        var http = new DefaultHttpContext();
        if (userId is not null)
            http.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString())], "test"));
        var stub = new StubService();
        return (new GoodsReceiptController(stub, tenant) { ControllerContext = new ControllerContext { HttpContext = http } }, orgId, stub);
    }

    private sealed class StubService : IGoodsReceiptService
    {
        public Guid? LastReceivedBy { get; private set; }
        public string? Error { get; set; }

        public Task<IReadOnlyList<GoodsReceiptResponse>> GetAllAsync(Guid o, Guid? purchaseOrderId, CancellationToken c) =>
            Task.FromResult<IReadOnlyList<GoodsReceiptResponse>>([]);
        public Task<GoodsReceiptResponse?> GetByIdAsync(Guid o, Guid id, CancellationToken c) =>
            Task.FromResult<GoodsReceiptResponse?>(null);
        public Task<GoodsReceiptResponse> CreateAsync(Guid o, Guid userId, CreateGoodsReceiptRequest r, CancellationToken c)
        {
            switch (Error)
            {
                case "argument": throw new ArgumentException("receivedQuantity cannot exceed the pending quantity.");
                case "notfound": throw new KeyNotFoundException("Purchase order does not exist.");
                case "conflict": throw new InvalidOperationException("Purchase order cannot receive goods.");
            }
            LastReceivedBy = userId;
            return Task.FromResult(new GoodsReceiptResponse(Guid.NewGuid(), 1, r.PurchaseOrderId!.Value, userId, DateTime.UtcNow,
                null, PurchaseOrderStatus.Received, []));
        }
    }
}
