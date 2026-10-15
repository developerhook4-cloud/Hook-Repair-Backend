using HookRepair.Modules.Auth.Application.Security;
using HookRepair.Modules.Catalogs.Application.Services;
using HookRepair.Modules.Catalogs.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace HookRepair.Modules.Catalogs.Infrastructure.Services;

public sealed class ServiceService(
    CatalogDbContext dbContext,
    ITenantContext tenantContext) : IServiceService
{
    public async Task<IReadOnlyList<ServiceResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var organizationId = EnsureAuthenticatedTenant();

        var services = await dbContext.Services
            .AsNoTracking()
            .Where(item => item.OrganizationId == organizationId)
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);

        return services.Select(Map).ToArray();
    }

    public async Task<ServiceResponse?> GetByIdAsync(Guid serviceId, CancellationToken cancellationToken = default)
    {
        var organizationId = EnsureAuthenticatedTenant();

        var service = await dbContext.Services
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == serviceId && item.OrganizationId == organizationId,
                cancellationToken);

        return service is null ? null : Map(service);
    }

    public async Task<ServiceResponse> CreateAsync(SaveServiceRequest request, CancellationToken cancellationToken = default)
    {
        var organizationId = EnsureAuthenticatedTenant();

        var service = new Service
        {
            OrganizationId = organizationId,
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            CategoryId = request.CategoryId,
            BrandId = request.BrandId,
            ModelId = request.ModelId,
            RetailPrice = request.RetailPrice,
            PublicPrice = request.PublicPrice,
            DistributorPrice = request.DistributorPrice,
            Active = true
        };

        dbContext.Services.Add(service);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Map(service);
    }

    public async Task<ServiceResponse?> UpdateAsync(Guid serviceId, SaveServiceRequest request, CancellationToken cancellationToken = default)
    {
        var organizationId = EnsureAuthenticatedTenant();

        var service = await dbContext.Services
            .SingleOrDefaultAsync(
                item => item.Id == serviceId && item.OrganizationId == organizationId,
                cancellationToken);

        if (service is null) return null;

        service.Name = request.Name.Trim();
        service.Description = request.Description.Trim();

        /*
        service.CategoryId = request.CategoryId;
        service.BrandId = request.BrandId;
        service.ModelId = request.ModelId;
        */

        service.RetailPrice = request.RetailPrice;
        service.PublicPrice = request.PublicPrice;
        service.DistributorPrice = request.DistributorPrice;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Map(service);
    }
    public async Task UpdateStatusAsync(Guid serviceId, bool active, CancellationToken cancellationToken = default)
    {
        var organizationId = EnsureAuthenticatedTenant();

        var service = await dbContext.Services
            .SingleOrDefaultAsync(
                item => item.Id == serviceId && item.OrganizationId == organizationId,
                cancellationToken);

        if (service is null) return;

        service.Active = active;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private Guid EnsureAuthenticatedTenant()
    {
        if (!tenantContext.IsAuthenticated)
            throw new InvalidOperationException("An authenticated organization is required.");

        return tenantContext.OrganizationId;
    }

    private static ServiceResponse Map(Service service) => new(
        service.Id,
        service.Name,
        service.Description ?? "",
        service.CategoryId,
        service.BrandId,
        service.ModelId,
        service.RetailPrice,
        service.PublicPrice,
        service.DistributorPrice,
        service.Active);
}