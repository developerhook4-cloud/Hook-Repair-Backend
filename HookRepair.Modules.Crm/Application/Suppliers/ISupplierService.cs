namespace HookRepair.Modules.Crm.Application.Suppliers;

public interface ISupplierService
{
    Task<IReadOnlyList<SupplierResponse>> GetAllAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<SupplierResponse?> GetByIdAsync(Guid organizationId, Guid supplierId, CancellationToken cancellationToken = default);
    Task<SupplierResponse> CreateAsync(Guid organizationId, CreateSupplierRequest request, CancellationToken cancellationToken = default);
    Task<SupplierResponse?> UpdateAsync(Guid organizationId, Guid supplierId, UpdateSupplierRequest request, CancellationToken cancellationToken = default);
    Task<SupplierResponse?> DeactivateAsync(Guid organizationId, Guid supplierId, CancellationToken cancellationToken = default);
}
