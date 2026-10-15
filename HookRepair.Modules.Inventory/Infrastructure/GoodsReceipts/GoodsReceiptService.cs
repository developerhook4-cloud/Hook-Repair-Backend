using HookRepair.Modules.Auth.Application.Security;
using HookRepair.Modules.Inventory.Application.GoodsReceipts;
using HookRepair.Modules.Inventory.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace HookRepair.Modules.Inventory.Infrastructure.GoodsReceipts;

public sealed class GoodsReceiptService(
    InventoryDbContext dbContext,
    ITenantContext tenantContext) : IGoodsReceiptService
{
    // Estados desde los que una orden todavía puede recibir mercancía.
    private static readonly HashSet<PurchaseOrderStatus> ReceivableStatuses =
    [
        PurchaseOrderStatus.Pending,
        PurchaseOrderStatus.Waiting,
        PurchaseOrderStatus.InProgress,
        PurchaseOrderStatus.ReceivedPartially
    ];

    public async Task<IReadOnlyList<GoodsReceiptResponse>> GetAllAsync(Guid organizationId, Guid? purchaseOrderId = null, CancellationToken cancellationToken = default)
    {
        EnsureTenant(organizationId);
        var query = Receipts().Where(receipt => receipt.OrganizationId == organizationId);
        if (purchaseOrderId is not null) query = query.Where(receipt => receipt.PurchaseOrderId == purchaseOrderId);

        var receipts = await query.OrderByDescending(receipt => receipt.CreatedAtUtc).ToListAsync(cancellationToken);
        return receipts.Select(Map).ToArray();
    }

    public async Task<GoodsReceiptResponse?> GetByIdAsync(Guid organizationId, Guid goodsReceiptId, CancellationToken cancellationToken = default)
    {
        EnsureTenant(organizationId);
        var receipt = await Receipts()
            .SingleOrDefaultAsync(item => item.Id == goodsReceiptId && item.OrganizationId == organizationId, cancellationToken);
        return receipt is null ? null : Map(receipt);
    }

    public async Task<GoodsReceiptResponse> CreateAsync(Guid organizationId, Guid receivedByUserId, CreateGoodsReceiptRequest request, CancellationToken cancellationToken = default)
    {
        EnsureTenant(organizationId);
        if (receivedByUserId == Guid.Empty) throw new ArgumentException("Authenticated user id is required.");
        var validation = GoodsReceiptRequestValidator.Validate(request);
        if (validation is not null) throw new ArgumentException(validation);
        var purchaseOrderId = request.PurchaseOrderId!.Value;

        // En PostgreSQL se bloquea la orden para que dos recepciones simultáneas no reciban de más.
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;
        if (transaction is not null)
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM inventory.purchase_orders WHERE id = {purchaseOrderId} AND organization_id = {organizationId} FOR UPDATE",
                cancellationToken);

        var order = await dbContext.PurchaseOrders
            .Include(item => item.Lines).ThenInclude(line => line.Product)
            .SingleOrDefaultAsync(item => item.Id == purchaseOrderId && item.OrganizationId == organizationId, cancellationToken)
            ?? throw new KeyNotFoundException("Purchase order does not exist.");
        if (!ReceivableStatuses.Contains(order.Status))
            throw new InvalidOperationException($"Purchase order with status {order.Status} cannot receive goods.");

        var lineIds = order.Lines.Select(line => line.Id).ToArray();
        var previouslyReceived = await dbContext.GoodsReceiptLines.AsNoTracking()
            .Where(line => lineIds.Contains(line.PurchaseOrderLineId))
            .GroupBy(line => line.PurchaseOrderLineId)
            .Select(group => new { group.Key, Quantity = group.Sum(line => line.ReceivedQuantity) })
            .ToDictionaryAsync(item => item.Key, item => item.Quantity, cancellationToken);

        var requested = request.Lines?.ToDictionary(line => line.PurchaseOrderLineId!.Value, line => line.ReceivedQuantity);
        if (requested is not null)
        {
            var unknown = requested.Keys.FirstOrDefault(id => !lineIds.Contains(id));
            if (unknown != Guid.Empty) throw new ArgumentException($"purchaseOrderLineId {unknown} does not belong to the purchase order.");
        }

        var receipt = new GoodsReceipt
        {
            OrganizationId = organizationId,
            PurchaseOrderId = order.Id,
            ReceivedByUserId = receivedByUserId,
            CreatedAtUtc = DateTime.UtcNow,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
        };

        var fullyReceived = true;
        foreach (var orderLine in order.Lines)
        {
            var pending = orderLine.Quantity - previouslyReceived.GetValueOrDefault(orderLine.Id);
            if (pending <= 0) continue;

            // Autocompleta con lo pendiente; el usuario solo envía la línea si hubo discrepancia.
            var quantity = requested is not null && requested.TryGetValue(orderLine.Id, out var value) && value is not null
                ? value.Value
                : pending;
            if (quantity > pending)
                throw new ArgumentException($"receivedQuantity for product {orderLine.Product.Name} cannot exceed the pending quantity ({pending}).");
            if (quantity < pending) fullyReceived = false;
            if (quantity == 0) continue;
            if (orderLine.Product.IsSerialized)
                throw new ArgumentException($"Product {orderLine.Product.Name} is serialized; receiving serialized products is not supported yet.");

            receipt.Lines.Add(new GoodsReceiptLine
            {
                OrganizationId = organizationId,
                GoodsReceiptId = receipt.Id,
                PurchaseOrderLineId = orderLine.Id,
                ProductId = orderLine.ProductId,
                OrderedQuantity = orderLine.Quantity,
                ReceivedQuantity = quantity
            });

            var product = orderLine.Product;
            int currentStock;
            try
            {
                currentStock = checked(product.InStock + quantity);
            }
            catch (OverflowException)
            {
                throw new InvalidOperationException("The resulting stock is invalid.");
            }

            dbContext.InventoryMovements.Add(new InventoryMovement
            {
                OrganizationId = organizationId,
                ProductId = product.Id,
                Type = InventoryMovementType.Entry,
                Quantity = quantity,
                PreviousStock = product.InStock,
                CurrentStock = currentStock,
                Reason = $"Recepción de orden de compra #{order.PurchaseOrderNumber}",
                CreatedAtUtc = receipt.CreatedAtUtc
            });
            product.InStock = currentStock;
        }

        if (receipt.Lines.Count == 0) throw new ArgumentException("The goods receipt must receive at least one unit.");

        order.Status = fullyReceived ? PurchaseOrderStatus.Received : PurchaseOrderStatus.ReceivedPartially;
        dbContext.GoodsReceipts.Add(receipt);
        await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);

        return (await GetByIdAsync(organizationId, receipt.Id, cancellationToken))!;
    }

    private IQueryable<GoodsReceipt> Receipts() =>
        dbContext.GoodsReceipts.AsNoTracking()
            .Include(receipt => receipt.PurchaseOrder)
            .Include(receipt => receipt.Lines).ThenInclude(line => line.Product);

    private void EnsureTenant(Guid organizationId)
    {
        if (!tenantContext.IsAuthenticated || tenantContext.OrganizationId != organizationId)
            throw new TenantAccessException();
    }

    private static GoodsReceiptResponse Map(GoodsReceipt receipt) =>
        new(receipt.Id, receipt.GoodsReceiptNumber, receipt.PurchaseOrderId, receipt.ReceivedByUserId, receipt.CreatedAtUtc,
            receipt.Notes, receipt.PurchaseOrder.Status,
            receipt.Lines.OrderBy(line => line.Product.Name)
                .Select(line => new GoodsReceiptLineResponse(line.Id, line.PurchaseOrderLineId, line.ProductId, line.Product.Name,
                    line.OrderedQuantity, line.ReceivedQuantity))
                .ToArray());
}
