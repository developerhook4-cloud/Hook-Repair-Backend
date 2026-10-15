namespace HookRepair.Modules.Crm.Application.Clients;

public interface IClientService
{
    Task<IReadOnlyList<ClientResponse>> GetAllAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<ClientResponse?> GetByIdAsync(Guid organizationId, Guid clientId, CancellationToken cancellationToken = default);
    Task<ClientResponse> CreateAsync(Guid organizationId, CreateClientRequest request, CancellationToken cancellationToken = default);
    Task<ClientResponse?> UpdateAsync(Guid organizationId, Guid clientId, UpdateClientRequest request, CancellationToken cancellationToken = default);
    Task<ClientResponse?> DeactivateAsync(Guid organizationId, Guid clientId, CancellationToken cancellationToken = default);
}
