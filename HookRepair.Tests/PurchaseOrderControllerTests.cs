using System.Reflection;
using System.Security.Claims;
using FluentAssertions;
using HookRepair.Api.Controllers;
using HookRepair.Modules.Auth.Core.Constants;
using HookRepair.Modules.Auth.Infrastructure.Security;
using HookRepair.Modules.Inventory.Application.PurchaseOrders;
using HookRepair.Modules.Inventory.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HookRepair.Tests;

public sealed class PurchaseOrderControllerTests
{
    [Fact]
    public void Routes_and_policies_match_contract()
    {
        typeof(PurchaseOrderController).GetCustomAttribute<RouteAttribute>()!.Template
            .Should().Be("api/{orgId:guid}/purchase-orders");
        Policy(nameof(PurchaseOrderController.GetAll)).Should().Be(SystemPermissions.ViewInventory);
        Policy(nameof(PurchaseOrderController.GetById)).Should().Be(SystemPermissions.ViewInventory);
        Policy(nameof(PurchaseOrderController.Create)).Should().Be(SystemPermissions.UpdateInventory);
    }

    [Fact]
    public async Task Create_uses_authenticated_user_as_creator()
    {
        var userId = Guid.NewGuid();
        var (controller, orgId, stub) = Build(userId);

        var result = await controller.Create(orgId,
            new CreatePurchaseOrderRequest(Guid.NewGuid(), [new(Guid.NewGuid(), 1)]), CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>().Which.Value.Should().BeOfType<PurchaseOrderMutationResponse>();
        stub.LastCreatedBy.Should().Be(userId);
    }

    [Fact]
    public async Task Create_without_user_claim_is_unauthorized()
    {
        var (controller, orgId, _) = Build(null);

        var result = await controller.Create(orgId,
            new CreatePurchaseOrderRequest(Guid.NewGuid(), [new(Guid.NewGuid(), 1)]), CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Create_rejects_empty_products()
    {
        var (controller, orgId, _) = Build(Guid.NewGuid());

        var result = await controller.Create(orgId, new CreatePurchaseOrderRequest(Guid.NewGuid(), []), CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Create_maps_service_argument_error_to_400()
    {
        var (controller, orgId, stub) = Build(Guid.NewGuid());
        stub.ThrowArgument = true;

        var result = await controller.Create(orgId,
            new CreatePurchaseOrderRequest(Guid.NewGuid(), [new(Guid.NewGuid(), 1)]), CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task GetAll_rejects_other_organization()
    {
        var (controller, _, _) = Build(Guid.NewGuid());

        var result = await controller.GetAll(Guid.NewGuid(), CancellationToken.None);

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
        typeof(PurchaseOrderController).GetMethod(method)!.GetCustomAttribute<AuthorizeAttribute>()!.Policy;

    private static (PurchaseOrderController, Guid, StubService) Build(Guid? userId)
    {
        var orgId = Guid.NewGuid();
        var tenant = new TenantContext();
        tenant.Set(orgId);
        var http = new DefaultHttpContext();
        if (userId is not null)
            http.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString())], "test"));
        var stub = new StubService();
        return (new PurchaseOrderController(stub, tenant) { ControllerContext = new ControllerContext { HttpContext = http } }, orgId, stub);
    }

    private sealed class StubService : IPurchaseOrderService
    {
        public Guid? LastCreatedBy { get; private set; }
        public bool ThrowArgument { get; set; }

        public Task<IReadOnlyList<PurchaseOrderResponse>> GetAllAsync(Guid o, CancellationToken c) =>
            Task.FromResult<IReadOnlyList<PurchaseOrderResponse>>([]);
        public Task<PurchaseOrderResponse?> GetByIdAsync(Guid o, Guid id, CancellationToken c) =>
            Task.FromResult<PurchaseOrderResponse?>(null);
        public Task<PurchaseOrderResponse> CreateAsync(Guid o, Guid userId, CreatePurchaseOrderRequest r, CancellationToken c)
        {
            if (ThrowArgument) throw new ArgumentException("supplierId does not exist or is inactive.");
            LastCreatedBy = userId;
            return Task.FromResult(new PurchaseOrderResponse(Guid.NewGuid(), Guid.NewGuid(), userId, DateTime.UtcNow,
                PurchaseOrderStatus.Pending, PurchasePaymentStatus.Unpaid, 1m, []));
        }
    }
}
