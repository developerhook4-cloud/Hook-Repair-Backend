using HookRepair.Modules.Inventory.Core.Entities;

namespace HookRepair.Modules.Inventory.Application.PurchaseOrders;

public sealed record PurchaseOrderProductRequest(Guid? ProductId, int Quantity);

public sealed record CreatePurchaseOrderRequest(Guid? SupplierId, IReadOnlyList<PurchaseOrderProductRequest>? Products);

public sealed record PurchaseOrderLineResponse(
    Guid Id, Guid ProductId, string ProductName, int Quantity, decimal UnitCost, decimal Subtotal);

public sealed record PurchaseOrderResponse(
    Guid Id,
    int PurchaseOrderNumber,
    Guid SupplierId,
    string SupplierName,
    Guid CreatedByUserId,
    string UserName,
    DateTime CreatedAtUtc,
    PurchaseOrderStatus Status,
    PurchasePaymentStatus PaymentStatus,
    decimal Total,
    IReadOnlyList<PurchaseOrderLineResponse> Lines);

public sealed record PurchaseOrderMutationResponse(Guid Id, string Message);

public static class PurchaseOrderRequestValidator
{
    public const int MaxProducts = 100;
    public const int MaxQuantity = 100_000;

    public static string? Validate(CreatePurchaseOrderRequest? request)
    {
        if (request is null) return "Request body is required.";
        if (request.SupplierId is null || request.SupplierId == Guid.Empty) return "supplierId is required.";
        if (request.Products is null || request.Products.Count == 0 || request.Products.Count > MaxProducts)
            return $"products must contain between 1 and {MaxProducts} items.";

        var seen = new HashSet<Guid>();
        foreach (var product in request.Products)
        {
            if (product.ProductId is null || product.ProductId == Guid.Empty) return "productId is required for every product.";
            if (!seen.Add(product.ProductId.Value)) return "products cannot contain duplicated productId values.";
            if (product.Quantity < 1 || product.Quantity > MaxQuantity)
                return $"quantity must be between 1 and {MaxQuantity}.";
        }

        return null;
    }
}
