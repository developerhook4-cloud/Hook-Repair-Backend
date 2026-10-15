using System.Reflection;
using FluentAssertions;
using HookRepair.Api.Controllers;
using HookRepair.Modules.Auth.Core.Constants;
using HookRepair.Modules.Auth.Infrastructure.Security;
using HookRepair.Modules.Inventory.Application.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HookRepair.Tests;

public sealed class ProductControllerTests
{
    [Fact]
    public void Routes_and_policies_match_contract()
    {
        typeof(ProductController).GetCustomAttribute<RouteAttribute>()!.Template
            .Should().Be("api/{orgId:guid}/products");
        Policy(nameof(ProductController.GetAll)).Should().Be(SystemPermissions.ViewInventory);
        Policy(nameof(ProductController.GetById)).Should().Be(SystemPermissions.ViewInventory);
        Policy(nameof(ProductController.Create)).Should().Be(SystemPermissions.UpdateInventory);
        Policy(nameof(ProductController.Update)).Should().Be(SystemPermissions.UpdateInventory);
        Policy(nameof(ProductController.UpdateStatus)).Should().Be(SystemPermissions.DeleteInventory);
    }

    [Fact]
    public async Task GetAll_rejects_other_organization()
    {
        var (controller, _) = Build();
        var result = await controller.GetAll(Guid.NewGuid(), CancellationToken.None);
        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task Create_returns_201_with_mutation_response()
    {
        var (controller, orgId) = Build();
        var request = new ProductRequest("A", null, null, null, null, null, null, null, null, 0, 0, 0, 1m, 1m, 1m, false, true);

        var result = await controller.Create(orgId, request, CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.Value.Should().BeOfType<ProductMutationResponse>();
    }

    [Fact]
    public async Task Create_maps_validation_error_to_400()
    {
        var (controller, orgId) = Build();
        var request = new ProductRequest(" ", null, null, null, null, null, null, null, null, 0, 0, 0, 1m, 1m, 1m, false, true);

        var result = await controller.Create(orgId, request, CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Update_missing_returns_404()
    {
        var (controller, orgId) = Build();
        var request = new ProductRequest("A", null, null, null, null, null, null, null, null, 0, 0, 0, 1m, 1m, 1m, false, true);

        var result = await controller.Update(orgId, Guid.NewGuid(), request, CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(404);
    }

    private static string? Policy(string method) =>
        typeof(ProductController).GetMethod(method)!.GetCustomAttribute<AuthorizeAttribute>()!.Policy;

    private static (ProductController Controller, Guid OrgId) Build()
    {
        var orgId = Guid.NewGuid();
        var tenant = new TenantContext();
        tenant.Set(orgId);
        return (new ProductController(new StubProductService(), tenant)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        }, orgId);
    }

    private sealed class StubProductService : IProductService
    {
        private static ProductResponse Sample(Guid id) =>
            new(id, "A", null, null, null, [], null, "", null, null, 0, 0, 0, 1m, 1m, 1m, false, true);
        public Task<IReadOnlyList<ProductResponse>> GetAllAsync(Guid o, CancellationToken c) =>
            Task.FromResult<IReadOnlyList<ProductResponse>>([]);
        public Task<ProductResponse?> GetByIdAsync(Guid o, Guid id, CancellationToken c) =>
            Task.FromResult<ProductResponse?>(null);
        public Task<ProductResponse> CreateAsync(Guid o, ProductRequest r, CancellationToken c) =>
            Task.FromResult(Sample(Guid.NewGuid()));
        public Task<ProductResponse?> UpdateAsync(Guid o, Guid id, ProductRequest r, CancellationToken c) =>
            Task.FromResult<ProductResponse?>(null);
        public Task<ProductResponse?> UpdateStatusAsync(Guid o, Guid id, bool a, CancellationToken c) =>
            Task.FromResult<ProductResponse?>(null);
    }
}
