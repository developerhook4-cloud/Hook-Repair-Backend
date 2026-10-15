using HookRepair.Modules.Catalogs.Application.Services;

namespace HookRepair.Modules.Catalogs.Application.Services;

public interface IServiceService
{
    Task<IReadOnlyList<ServiceResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ServiceResponse?> GetByIdAsync(Guid serviceId, CancellationToken cancellationToken = default);
    Task<ServiceResponse> CreateAsync(SaveServiceRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResponse?> UpdateAsync(Guid serviceId, SaveServiceRequest request, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(Guid serviceId, bool active, CancellationToken cancellationToken = default);
}