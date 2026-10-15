namespace HookRepair.Modules.Inventory.Application.Products;

public sealed record ProductRequest(
    string? Name,
    Guid? CategoryId,
    Guid? BrandId,
    Guid? ModelId,
    IReadOnlyList<Guid>? ModelIds,
    Guid? SupplierId,
    string? Upc,
    string? ImageUrl,
    string? Condition,
    int? InStock,
    int? StockWarning,
    int? MinimumReorderPoint,
    decimal? Price,
    decimal? GeneralSalePrice,
    decimal? DistributorSalePrice,
    bool? IsSerialized,
    bool? Active)
{
    // modelId (frontend) + modelIds (backend) unificados, sin duplicados ni Guid.Empty.
    public IReadOnlyList<Guid> AllModelIds() =>
        (ModelIds ?? []).Append(ModelId ?? Guid.Empty)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();
}

public sealed record ProductResponse(
    Guid Id,
    string Name,
    Guid? CategoryId,
    string? CategoryName,
    Guid? BrandId,
    string? BrandName,
    Guid? ModelId,
    string? ModelName,
    IReadOnlyList<string> ModelNames,
    Guid? SupplierId,
    string? SupplierName,
    string Upc,
    string? ImageUrl,
    string? Condition,
    int InStock,
    int StockWarning,
    int MinimumReorderPoint,
    decimal Price,
    decimal GeneralSalePrice,
    decimal DistributorSalePrice,
    bool IsSerialized,
    IReadOnlyList<String> SerialNumbers,
    bool Active,
    string StockStatus);

public sealed record ProductMutationResponse(Guid Id, string Message);

public sealed record UpdateProductStatusRequest(bool Active);

public static class ProductRequestValidator
{
    public static string? ValidateCreate(ProductRequest? request)
    {
        var common = ValidateCommon(request);
        if (common is not null) return common;

        if (request!.InStock is < 0) return "inStock must be zero or greater.";
        if (request.IsSerialized == true && request.InStock is > 0)
            return "inStock must be 0 for serialized products; serialized units are registered individually.";
        return null;
    }

    public static string? ValidateUpdate(ProductRequest? request) => ValidateCommon(request);

    private static string? ValidateCommon(ProductRequest? request)
    {
        if (request is null) return "Request body is required.";

        var name = request.Name?.Trim();
        if (string.IsNullOrEmpty(name) || name.Length > 250)
            return "name is required and must contain 1 to 250 characters.";

        foreach (var (field, value) in new[]
                 {
                     ("price", request.Price),
                     ("generalSalePrice", request.GeneralSalePrice),
                     ("distributorSalePrice", request.DistributorSalePrice)
                 })
        {
            if (value is null || value < 0 || decimal.Round(value.Value, 2) != value.Value)
                return $"{field} is required, must be zero or greater and have at most 2 decimals.";
        }

        if (request.StockWarning is < 0) return "stockWarning must be zero or greater.";
        if (request.MinimumReorderPoint is < 0) return "minimumReorderPoint must be zero or greater.";

        if (request.Condition?.Trim().Length > 100)
            return "condition cannot contain more than 100 characters.";

        if (!string.IsNullOrWhiteSpace(request.ImageUrl) &&
            (request.ImageUrl.Length > 2048 ||
             !Uri.TryCreate(request.ImageUrl, UriKind.Absolute, out var uri) ||
             (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
            return "imageUrl must be an absolute http or https URL up to 2048 characters.";

        return null;
    }
}
