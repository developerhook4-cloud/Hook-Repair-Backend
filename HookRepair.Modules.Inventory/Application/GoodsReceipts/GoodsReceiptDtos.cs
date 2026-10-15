using System.Text.Json.Serialization;
using HookRepair.Modules.Inventory.Core.Entities;

namespace HookRepair.Modules.Inventory.Application.GoodsReceipts;

public sealed record GoodsReceiptLineRequest(Guid? PurchaseOrderLineId, int? ReceivedQuantity);

// Lines null u omitida: se recibe todo lo pendiente de la orden (autocompletado).
public sealed record CreateGoodsReceiptRequest(Guid? PurchaseOrderId, IReadOnlyList<GoodsReceiptLineRequest>? Lines, string? Notes);

public sealed record GoodsReceiptLineResponse(
    Guid Id, Guid PurchaseOrderLineId, Guid ProductId, string ProductName, int OrderedQuantity, int ReceivedQuantity);

public sealed record GoodsReceiptResponse(
    Guid Id,
    int GoodsReceiptNumber,
    Guid PurchaseOrderId,
    Guid ReceivedByUserId,
    DateTime CreatedAtUtc,
    string? Notes,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] PurchaseOrderStatus PurchaseOrderStatus,
    IReadOnlyList<GoodsReceiptLineResponse> Lines);

public sealed record GoodsReceiptMutationResponse(Guid Id, string Message);

public static class GoodsReceiptRequestValidator
{
    public const int MaxLines = 100;
    public const int MaxQuantity = 100_000;
    public const int MaxNotesLength = 500;

    public static string? Validate(CreateGoodsReceiptRequest? request)
    {
        if (request is null) return "Request body is required.";
        if (request.PurchaseOrderId is null || request.PurchaseOrderId == Guid.Empty) return "purchaseOrderId is required.";
        if (request.Notes is { Length: > MaxNotesLength }) return $"notes cannot exceed {MaxNotesLength} characters.";
        if (request.Lines is null) return null;
        if (request.Lines.Count > MaxLines) return $"lines cannot contain more than {MaxLines} items.";

        var seen = new HashSet<Guid>();
        foreach (var line in request.Lines)
        {
            if (line.PurchaseOrderLineId is null || line.PurchaseOrderLineId == Guid.Empty)
                return "purchaseOrderLineId is required for every line.";
            if (!seen.Add(line.PurchaseOrderLineId.Value)) return "lines cannot contain duplicated purchaseOrderLineId values.";
            if (line.ReceivedQuantity is < 0 or > MaxQuantity)
                return $"receivedQuantity must be between 0 and {MaxQuantity}.";
        }

        return null;
    }
}
