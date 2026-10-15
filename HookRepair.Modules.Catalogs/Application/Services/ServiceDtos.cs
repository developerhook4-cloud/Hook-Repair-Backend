using System.ComponentModel.DataAnnotations;

namespace HookRepair.Modules.Catalogs.Application.Services;

public sealed class SaveServiceRequest
{
    [Required, StringLength(200)]
    public string Name { get; init; } = string.Empty;

    [Required, StringLength(2000)]
    public string Description { get; init; } = string.Empty;

    public Guid? CategoryId { get; init; }

    public Guid? BrandId { get; init; }
    public Guid? ModelId { get; init; }

    [Range(0, 999999999)]
    public decimal RetailPrice { get; init; }

    [Range(0, 999999999)]
    public decimal PublicPrice { get; init; }

    [Range(0, 999999999)]
    public decimal DistributorPrice { get; init; }
}

public sealed record ServiceResponse(
    Guid Id,
    string Name,
    string Description,
    Guid? CategoryId,
    Guid? BrandId,
    Guid? ModelId,
    decimal RetailPrice,
    decimal PublicPrice,
    decimal DistributorPrice,
    bool Active
);
public sealed record ServiceMutationResponse(Guid Id, string Message);
public sealed record UpdateServiceStatusRequest(bool Active);