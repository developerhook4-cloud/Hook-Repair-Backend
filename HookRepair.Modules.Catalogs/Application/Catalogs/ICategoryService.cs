namespace HookRepair.Modules.Catalogs.Application.Catalogs;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryResponse>> GetAllAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<CategoryResponse?> GetByIdAsync(Guid organizationId, Guid categoryId, CancellationToken cancellationToken = default);
    Task<CategoryResponse> CreateAsync(Guid organizationId, CreateCategoryRequest request, CancellationToken cancellationToken = default);
    Task<CategoryResponse?> UpdateAsync(Guid organizationId, Guid categoryId, UpdateCategoryRequest request, CancellationToken cancellationToken = default);
    Task<CategoryResponse?> DeactivateAsync(Guid organizationId, Guid categoryId, CancellationToken cancellationToken = default);
}
