using System.Reflection;
using FluentAssertions;
using HookRepair.Api.Controllers;
using HookRepair.Modules.Auth.Application.Security;
using HookRepair.Modules.Auth.Core.Constants;
using HookRepair.Modules.Auth.Infrastructure.Security;
using HookRepair.Modules.Catalogs.Application.Catalogs;
using HookRepair.Modules.Crm.Application.Clients;
using HookRepair.Modules.Crm.Application.Suppliers;
using HookRepair.Modules.Crm.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HookRepair.Tests;

public sealed class CatalogAndThirdPartyControllerTests
{
    [Fact]
    public async Task Category_create_rejects_missing_name()
    {
        var organizationId = Guid.NewGuid();
        var tenant = new TenantContext();
        tenant.Set(organizationId);
        var controller = new CategoryController(new StubCategoryService(), tenant)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Create(
            organizationId,
            new CreateCategoryRequest(null, "icon"),
            CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Client_get_all_rejects_mismatched_organization()
    {
        var organizationId = Guid.NewGuid();
        var tenant = new TenantContext();
        tenant.Set(organizationId);
        var controller = new ClientController(new StubClientService(), tenant)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.GetAll(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task Category_get_by_id_returns_not_found_for_missing_record()
    {
        var organizationId = Guid.NewGuid();
        var tenant = new TenantContext();
        tenant.Set(organizationId);
        var controller = new CategoryController(new StubCategoryService(), tenant);

        var result = await controller.GetById(organizationId, Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Category_create_maps_unique_violation_to_conflict()
    {
        var organizationId = Guid.NewGuid();
        var tenant = new TenantContext();
        tenant.Set(organizationId);
        var controller = new CategoryController(new StubCategoryService(uniqueConflict: true), tenant);

        var result = await controller.Create(
            organizationId,
            new CreateCategoryRequest("Phones", "phone"),
            CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(409);
    }

    [Fact]
    public void Controllers_use_expected_routes_and_policies()
    {
        AssertController<CategoryController>("api/{orgId:guid}/categories", SystemPermissions.ViewCatalogs, SystemPermissions.UpdateCatalogs, SystemPermissions.DeleteCatalogs);
        AssertController<BrandController>("api/{orgId:guid}/brands", SystemPermissions.ViewCatalogs, SystemPermissions.UpdateCatalogs, SystemPermissions.DeleteCatalogs);
        AssertController<ModelController>("api/{orgId:guid}/models", SystemPermissions.ViewCatalogs, SystemPermissions.UpdateCatalogs, SystemPermissions.DeleteCatalogs);
        AssertController<ClientController>("api/{orgId:guid}/clients", SystemPermissions.ViewCustomers, SystemPermissions.UpdateCustomers, SystemPermissions.DeleteCustomers);
        AssertController<SupplierController>("api/{orgId:guid}/suppliers", SystemPermissions.ViewSuppliers, SystemPermissions.UpdateSuppliers, SystemPermissions.DeleteSuppliers);
    }

    private static void AssertController<T>(string route, string view, string update, string delete)
    {
        typeof(T).GetCustomAttributes<RouteAttribute>(true).Single().Template.Should().Be(route);
        typeof(T).GetCustomAttributes<AuthorizeAttribute>(true).Should().ContainSingle();
        var methods = typeof(T).GetMethods();
        methods.Single(item => item.Name == "GetAll").GetCustomAttributes<AuthorizeAttribute>()
            .Single(item => item.Policy is not null).Policy.Should().Be(view);
        methods.Single(item => item.Name == "Create").GetCustomAttributes<AuthorizeAttribute>()
            .Single(item => item.Policy is not null).Policy.Should().Be(update);
        methods.Single(item => item.Name == "Update").GetCustomAttributes<AuthorizeAttribute>()
            .Single(item => item.Policy is not null).Policy.Should().Be(update);
        methods.Single(item => item.Name == "Deactivate").GetCustomAttributes<AuthorizeAttribute>()
            .Single(item => item.Policy is not null).Policy.Should().Be(delete);
    }

    private sealed class StubCategoryService(bool uniqueConflict = false) : ICategoryService
    {
        public Task<IReadOnlyList<CategoryResponse>> GetAllAsync(Guid organizationId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CategoryResponse>>([]);
        public Task<CategoryResponse?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default) => Task.FromResult<CategoryResponse?>(null);
        public Task<CategoryResponse> CreateAsync(Guid organizationId, CreateCategoryRequest request, CancellationToken cancellationToken = default) => uniqueConflict
            ? throw new DbUpdateException("duplicate", new PostgresException("duplicate", "ERROR", "ERROR", PostgresErrorCodes.UniqueViolation))
            : Task.FromResult(new CategoryResponse(Guid.NewGuid(), request.Name ?? "", request.Icon ?? "", true));
        public Task<CategoryResponse?> UpdateAsync(Guid organizationId, Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken = default) => Task.FromResult<CategoryResponse?>(null);
        public Task<CategoryResponse?> DeactivateAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default) => Task.FromResult<CategoryResponse?>(null);
    }

    private sealed class StubClientService : IClientService
    {
        public Task<IReadOnlyList<ClientResponse>> GetAllAsync(Guid organizationId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ClientResponse>>([]);
        public Task<ClientResponse?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default) => Task.FromResult<ClientResponse?>(null);
        public Task<ClientResponse> CreateAsync(Guid organizationId, CreateClientRequest request, CancellationToken cancellationToken = default) => Task.FromResult(new ClientResponse(Guid.NewGuid(), request.Name ?? "", request.CustomerType, request.Phone1 ?? "", request.Phone2, request.LegalName, request.Email, null, request.TitularName, request.AuthorizedPeople, request.Rfc, request.TaxRegime, request.ExpenseType, true));
        public Task<ClientResponse?> UpdateAsync(Guid organizationId, Guid id, UpdateClientRequest request, CancellationToken cancellationToken = default) => Task.FromResult<ClientResponse?>(null);
        public Task<ClientResponse?> DeactivateAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default) => Task.FromResult<ClientResponse?>(null);
    }
}
