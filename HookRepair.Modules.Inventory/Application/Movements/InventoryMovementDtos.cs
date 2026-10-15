using System.Text.Json.Serialization;
using HookRepair.Modules.Inventory.Core.Entities;

namespace HookRepair.Modules.Inventory.Application.Movements;

public sealed record InventoryMovementResponse(
    Guid Id,
    Guid ProductId,
    InventoryMovementType Type,
    int Quantity,
    int PreviousStock,
    int CurrentStock,
    string? Reason,
    DateTime CreatedAtUtc);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateInventoryMovementRequest(
    Guid? ProductId,
    InventoryMovementType? Type,
    int Quantity,
    string? Reason);

public static class InventoryMovementRequestValidator
{
    public static string? Validate(CreateInventoryMovementRequest? request)
    {
        if (request is null)
            return "Request body is required.";

        if (request.ProductId is null || request.ProductId == Guid.Empty)
            return "productId is required.";

        if (request.Type is null || !Enum.IsDefined(request.Type.Value))
            return "type is required and must be valid.";

        if (request.Type is InventoryMovementType.Entry or InventoryMovementType.Exit &&
            request.Quantity <= 0)
            return "quantity must be greater than zero for entries and exits.";

        if (request.Type is InventoryMovementType.Adjustment &&
            request.Quantity == 0)
            return "quantity cannot be zero for an adjustment.";

        if (request.Reason?.Trim().Length > 500)
            return "reason cannot contain more than 500 characters.";

        return null;
    }
}