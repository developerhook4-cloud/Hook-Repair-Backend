namespace HookRepair.Modules.Inventory.Application.Products;

public interface IProductService
{
    Task<IReadOnlyList<ProductResponse>> GetAllAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<ProductResponse?> GetByIdAsync(Guid organizationId, Guid productId, CancellationToken cancellationToken = default);
    Task<ProductResponse> CreateAsync(Guid organizationId, ProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductResponse?> UpdateAsync(Guid organizationId, Guid productId, ProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductResponse?> UpdateStatusAsync(Guid organizationId, Guid productId, bool active, CancellationToken cancellationToken = default);
}
