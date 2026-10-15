using HookRepair.Modules.Auth.Application.Security;
using HookRepair.Modules.Crm.Application.Clients;
using HookRepair.Modules.Crm.Application.ThirdParties;
using HookRepair.Modules.Crm.Core.Entities;
using HookRepair.Shared.Core.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace HookRepair.Modules.Crm.Infrastructure.Clients;

public sealed class CustomerService(CrmDbContext dbContext, ITenantContext tenantContext) : IClientService
{
    public async Task<IReadOnlyList<ClientResponse>> GetAllAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        EnsureTenant(organizationId);
        var items = await dbContext.Customers.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId)
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);
        return items.Select(Map).ToArray();
    }

    public async Task<ClientResponse?> GetByIdAsync(Guid organizationId, Guid clientId, CancellationToken cancellationToken = default)
    {
        EnsureTenant(organizationId);
        var item = await dbContext.Customers.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == clientId && item.OrganizationId == organizationId, cancellationToken);
        return item is null ? null : Map(item);
    }

    public async Task<ClientResponse> CreateAsync(Guid organizationId, CreateClientRequest request, CancellationToken cancellationToken = default)
    {
        EnsureTenant(organizationId);
        Validate(request);
        var item = new Customer
        {
            OrganizationId = organizationId,
            CustomerType = request.CustomerType,
            Name = request.Name!.Trim(),
            Phone1 = request.Phone1!.Trim(),
            Phone2 = Normalize(request.Phone2),
            LegalName = Normalize(request.LegalName),
            Email = Normalize(request.Email),
            Address = Map(request.Address),
            TitularName = Normalize(request.TitularName),
            AuthorizedPeople = Normalize(request.AuthorizedPeople),
            Rfc = Normalize(request.Rfc),
            TaxRegime = Normalize(request.TaxRegime),
            ExpenseType = Normalize(request.ExpenseType),
            Active = true
        };
        dbContext.Customers.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task<ClientResponse?> UpdateAsync(Guid organizationId, Guid clientId, UpdateClientRequest request, CancellationToken cancellationToken = default)
    {
        EnsureTenant(organizationId);
        Validate(request);
        var item = await dbContext.Customers
            .SingleOrDefaultAsync(item => item.Id == clientId && item.OrganizationId == organizationId, cancellationToken);
        if (item is null) return null;
        item.CustomerType = request.CustomerType;
        item.Name = request.Name!.Trim();
        item.Phone1 = request.Phone1!.Trim();
        item.Phone2 = Normalize(request.Phone2);
        item.LegalName = Normalize(request.LegalName);
        item.Email = Normalize(request.Email);
        item.Address = Map(request.Address);
        item.TitularName = Normalize(request.TitularName);
        item.AuthorizedPeople = Normalize(request.AuthorizedPeople);
        item.Rfc = Normalize(request.Rfc);
        item.TaxRegime = Normalize(request.TaxRegime);
        item.ExpenseType = Normalize(request.ExpenseType);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    public async Task<ClientResponse?> DeactivateAsync(Guid organizationId, Guid clientId, CancellationToken cancellationToken = default)
    {
        EnsureTenant(organizationId);
        var item = await dbContext.Customers
            .SingleOrDefaultAsync(item => item.Id == clientId && item.OrganizationId == organizationId, cancellationToken);
        if (item is null) return null;
        item.Active = false;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(item);
    }

    private static void Validate(CreateClientRequest request) => ThrowIfInvalid(ThirdPartyRequestValidator.Validate(request));
    private static void Validate(UpdateClientRequest request) => ThrowIfInvalid(ThirdPartyRequestValidator.Validate(request));
    private static void ThrowIfInvalid(string? error) { if (error is not null) throw new ArgumentException(error); }
    private void EnsureTenant(Guid organizationId) { if (!tenantContext.IsAuthenticated || tenantContext.OrganizationId != organizationId) throw new TenantAccessException(); }

    private static AddressResponse? Map(FullAddress? address) => address is null
        ? null
        : new(address.Address, address.City, address.State, address.PostCode, address.Country);

    private static FullAddress? Map(AddressRequest? address) => address is null
        ? null
        : new()
        {
            Address = Normalize(address.Address),
            City = Normalize(address.City),
            State = Normalize(address.State),
            PostCode = Normalize(address.PostCode),
            Country = Normalize(address.Country)
        };

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string[]? Normalize(string[]? values) => values?
        .Select(Normalize)
        .Where(value => value is not null)
        .Select(value => value!)
        .ToArray();

    private static ClientResponse Map(Customer item) => new(
        item.Id,
        item.Name,
        item.CustomerType,
        item.Phone1,
        item.Phone2,
        item.LegalName,
        item.Email,
        Map(item.Address),
        item.TitularName,
        item.AuthorizedPeople,
        item.Rfc,
        item.TaxRegime,
        item.ExpenseType,
        item.Active);
}
