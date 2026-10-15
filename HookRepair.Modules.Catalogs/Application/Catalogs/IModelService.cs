namespace HookRepair.Modules.Catalogs.Application.Catalogs;

public interface IModelService
{
    Task<IReadOnlyList<ModelResponse>> GetAllAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<ModelResponse?> GetByIdAsync(Guid organizationId, Guid modelId, CancellationToken cancellationToken = default);
    Task<ModelResponse> CreateAsync(Guid organizationId, CreateModelRequest request, CancellationToken cancellationToken = default);
    Task<ModelResponse?> UpdateAsync(Guid organizationId, Guid modelId, UpdateModelRequest request, CancellationToken cancellationToken = default);
    Task<ModelResponse?> DeactivateAsync(Guid organizationId, Guid modelId, CancellationToken cancellationToken = default);
}
