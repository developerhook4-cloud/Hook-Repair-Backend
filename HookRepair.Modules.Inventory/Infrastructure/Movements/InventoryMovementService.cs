using HookRepair.Modules.Auth.Application.Security;
using HookRepair.Modules.Inventory.Application.Movements;
using HookRepair.Modules.Inventory.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace HookRepair.Modules.Inventory.Infrastructure.Movements;

public sealed class InventoryMovementService(
    InventoryDbContext dbContext,
    ITenantContext tenantContext) : IInventoryMovementService
{
    public async Task<IReadOnlyList<InventoryMovementResponse>> GetAllAsync(
        Guid organizationId,
        Guid? productId = null,
        CancellationToken cancellationToken = default)
    {
        EnsureTenant(organizationId);

        var query = dbContext.InventoryMovements
            .AsNoTracking()
            .Where(item => item.OrganizationId == organizationId);

        if (productId is not null)
            query = query.Where(item => item.ProductId == productId);

        var movements = await query
            .OrderByDescending(item => item.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return movements.Select(Map).ToArray();
    }

    public async Task<InventoryMovementResponse?> GetByIdAsync(
        Guid organizationId,
        Guid movementId,
        CancellationToken cancellationToken = default)
    {
        EnsureTenant(organizationId);

        var movement = await dbContext.InventoryMovements
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == movementId &&
                        item.OrganizationId == organizationId,
                cancellationToken);

        return movement is null ? null : Map(movement);
    }

    public async Task<InventoryMovementResponse> CreateAsync(
        Guid organizationId,
        CreateInventoryMovementRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureTenant(organizationId);

        var validation = InventoryMovementRequestValidator.Validate(request);
        if (validation is not null)
            throw new ArgumentException(validation);

        var product = await dbContext.Products.SingleOrDefaultAsync(
            item => item.Id == request.ProductId &&
                    item.OrganizationId == organizationId,
            cancellationToken);

        if (product is null)
            throw new KeyNotFoundException("Product does not exist.");

        var type = request.Type!.Value;
        var delta = type switch
        {
            InventoryMovementType.Entry => request.Quantity,
            InventoryMovementType.Exit => -request.Quantity,
            InventoryMovementType.Adjustment => request.Quantity,
            _ => throw new ArgumentException("Movement type is invalid.")
        };

        int currentStock;
        try
        {
            currentStock = checked(product.InStock + delta);
        }
        catch (OverflowException)
        {
            throw new InvalidOperationException("The resulting stock is invalid.");
        }

        if (currentStock < 0)
            throw new InvalidOperationException("Insufficient stock for this movement.");

        var movement = new InventoryMovement
        {
            OrganizationId = organizationId,
            ProductId = product.Id,
            Type = type,
            Quantity = request.Quantity,
            PreviousStock = product.InStock,
            CurrentStock = currentStock,
            Reason = string.IsNullOrWhiteSpace(request.Reason)
                ? null
                : request.Reason.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        product.InStock = currentStock;
        dbContext.InventoryMovements.Add(movement);

        // EF Core guarda la actualización del producto y el movimiento
        // como una sola operación de base de datos.
        await dbContext.SaveChangesAsync(cancellationToken);

        return Map(movement);
    }

    private void EnsureTenant(Guid organizationId)
    {
        if (!tenantContext.IsAuthenticated ||
            tenantContext.OrganizationId != organizationId)
        {
            throw new TenantAccessException();
        }
    }

    private static InventoryMovementResponse Map(InventoryMovement item) =>
        new(
            item.Id,
            item.ProductId,
            item.Type,
            item.Quantity,
            item.PreviousStock,
            item.CurrentStock,
            item.Reason,
            item.CreatedAtUtc);
}