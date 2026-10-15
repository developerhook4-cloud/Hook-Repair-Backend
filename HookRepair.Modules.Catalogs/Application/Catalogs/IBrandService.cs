namespace HookRepair.Modules.Catalogs.Application.Catalogs;

public interface IBrandService
{
    Task<IReadOnlyList<BrandResponse>> GetAllAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<BrandResponse?> GetByIdAsync(Guid organizationId, Guid brandId, CancellationToken cancellationToken = default);
    Task<BrandResponse> CreateAsync(Guid organizationId, CreateBrandRequest request, CancellationToken cancellationToken = default);
    Task<BrandResponse?> UpdateAsync(Guid organizationId, Guid brandId, UpdateBrandRequest request, CancellationToken cancellationToken = default);
    Task<BrandResponse?> DeactivateAsync(Guid organizationId, Guid brandId, CancellationToken cancellationToken = default);
}
