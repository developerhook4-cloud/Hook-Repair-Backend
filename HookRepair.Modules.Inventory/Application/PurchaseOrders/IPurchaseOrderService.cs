namespace HookRepair.Modules.Inventory.Application.PurchaseOrders;

public interface IPurchaseOrderService
{
    Task<IReadOnlyList<PurchaseOrderResponse>> GetAllAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<PurchaseOrderResponse?> GetByIdAsync(Guid organizationId, Guid purchaseOrderId, CancellationToken cancellationToken = default);
    Task<PurchaseOrderResponse> CreateAsync(Guid organizationId, Guid createdByUserId, CreatePurchaseOrderRequest request, CancellationToken cancellationToken = default);
}
