namespace HookRepair.Modules.Inventory.Application.GoodsReceipts;

public interface IGoodsReceiptService
{
    Task<IReadOnlyList<GoodsReceiptResponse>> GetAllAsync(Guid organizationId, Guid? purchaseOrderId = null, CancellationToken cancellationToken = default);
    Task<GoodsReceiptResponse?> GetByIdAsync(Guid organizationId, Guid goodsReceiptId, CancellationToken cancellationToken = default);
    Task<GoodsReceiptResponse> CreateAsync(Guid organizationId, Guid receivedByUserId, CreateGoodsReceiptRequest request, CancellationToken cancellationToken = default);
}
