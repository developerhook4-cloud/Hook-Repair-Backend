using System.Text.Json.Serialization;

namespace HookRepair.Modules.Catalogs.Application.Catalogs;

public sealed record CategoryResponse(Guid Id, string Name, string Icon, bool Active);
public sealed record BrandResponse(Guid Id, string Name, bool Active);
public sealed record ModelResponse(Guid Id, Guid BrandId, string BrandName, string Name, bool Active);
public sealed record CatalogMutationResponse(Guid Id, string Message);
public sealed record CreateCategoryRequest(string Name, string Icon);
public sealed record UpdateCategoryRequest(string Name, string Icon);
public sealed record CreateBrandRequest(string Name);
public sealed record UpdateBrandRequest(string? Name);
public sealed record CreateModelRequest(string Name, Guid BrandId);
public sealed record UpdateModelRequest(string Name, Guid BrandId);

public static class CatalogRequestValidator
{
    public static string? Validate(CreateCategoryRequest? request) => ValidateName(request?.Name);
    public static string? Validate(UpdateCategoryRequest? request) => ValidateName(request?.Name);
    public static string? Validate(CreateBrandRequest? request) => ValidateName(request?.Name);
    public static string? Validate(UpdateBrandRequest? request) => ValidateName(request?.Name);
    public static string? Validate(CreateModelRequest? request) => ValidateModel(request?.Name, request?.BrandId);
    public static string? Validate(UpdateModelRequest? request) => ValidateModel(request?.Name, request?.BrandId);

    private static string? ValidateModel(string? name, Guid? brandId)
    {
        var nameError = ValidateName(name);
        return nameError ?? (brandId is null || brandId == Guid.Empty
            ? "brandId is required."
            : null);
    }

    private static string? ValidateName(string? name) =>
        string.IsNullOrWhiteSpace(name) || name.Trim().Length > 150
            ? "name must contain between 1 and 150 characters."
            : null;
}
