namespace HookRepair.Modules.Inventory.Application.Movements;

public interface IInventoryMovementService
{
    Task<IReadOnlyList<InventoryMovementResponse>> GetAllAsync(
        Guid organizationId,
        Guid? productId = null,
        CancellationToken cancellationToken = default);

    Task<InventoryMovementResponse?> GetByIdAsync(
        Guid organizationId,
        Guid movementId,
        CancellationToken cancellationToken = default);

    Task<InventoryMovementResponse> CreateAsync(
        Guid organizationId,
        CreateInventoryMovementRequest request,
        CancellationToken cancellationToken = default);
}