using HookRepair.Modules.Auth.Application.Security;
using HookRepair.Modules.Crm.Application.Suppliers;
using HookRepair.Modules.Crm.Core.Entities;
using HookRepair.Modules.Inventory.Application.PurchaseOrders;
using HookRepair.Modules.Inventory.Core.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace HookRepair.Modules.Inventory.Infrastructure.PurchaseOrders;

public sealed class PurchaseOrderService(
    InventoryDbContext dbContext,
    ITenantContext tenantContext,
    ISupplierService suppliers) : IPurchaseOrderService
{
    public async Task<IReadOnlyList<PurchaseOrderResponse>> GetAllAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        EnsureTenant(organizationId);
        var orders = await dbContext.PurchaseOrders.AsNoTracking()
            .Include(order => order.Lines).ThenInclude(line => line.Product)
            .Where(order => order.OrganizationId == organizationId)
            .OrderByDescending(order => order.CreatedAtUtc)
            .Select(
                order => new PurchaseOrderResponse(
                    order.Id,
                    order.PurchaseOrderNumber,
                    order.SupplierId,
                    order.Supplier.Name,
                    order.CreatedByUserId,
                    order.User.Name,
                    order.CreatedAtUtc,
                    order.Status,
                    order.PaymentStatus,
                    order.Total,
                    order.Lines
                        .OrderBy(line => line.Product.Name)
                        .Select(line => new PurchaseOrderLineResponse(
                            line.Id,
                            line.ProductId,
                            line.Product.Name,
                            line.Quantity,
                            line.UnitCost,
                            line.Quantity * line.UnitCost))
                        .ToList()
                )
            )
            .ToListAsync(cancellationToken);
        return orders;
    }

    public async Task<PurchaseOrderResponse?> GetByIdAsync(Guid organizationId, Guid purchaseOrderId, CancellationToken cancellationToken = default)
    {
        EnsureTenant(organizationId);
        var order = await dbContext.PurchaseOrders.AsNoTracking()
            .Include(item => item.Lines).ThenInclude(line => line.Product)
            .SingleOrDefaultAsync(item => item.Id == purchaseOrderId && item.OrganizationId == organizationId, cancellationToken);
        return order is null ? null : Map(order);
    }

    public async Task<PurchaseOrderResponse> CreateAsync(Guid organizationId, Guid createdByUserId, CreatePurchaseOrderRequest request, CancellationToken cancellationToken = default)
    {
        EnsureTenant(organizationId);
        if (createdByUserId == Guid.Empty) throw new ArgumentException("Authenticated user id is required.");
        var validation = PurchaseOrderRequestValidator.Validate(request);
        if (validation is not null) throw new ArgumentException(validation);

        var supplier = await suppliers.GetByIdAsync(organizationId, request.SupplierId!.Value, cancellationToken);
        if (supplier is not { Active: true }) throw new ArgumentException("supplierId does not exist or is inactive.");

        var productIds = request.Products!.Select(item => item.ProductId!.Value).ToArray();
        var products = await dbContext.Products.AsNoTracking()
            .Where(product => productIds.Contains(product.Id) && product.OrganizationId == organizationId && product.Active)
            .ToDictionaryAsync(product => product.Id, cancellationToken);

        // Toda orden nueva nace Pendiente y Sin pagar; no modifica stock.
        var order = new PurchaseOrder
        {
            OrganizationId = organizationId,
            SupplierId = supplier.Id,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTime.UtcNow,
            Status = PurchaseOrderStatus.Pending,
            PaymentStatus = PurchasePaymentStatus.Unpaid
        };

        foreach (var item in request.Products!)
        {
            if (!products.TryGetValue(item.ProductId!.Value, out var product))
                throw new ArgumentException($"productId {item.ProductId} does not exist or is inactive.");
            order.Lines.Add(new PurchaseOrderLine
            {
                OrganizationId = organizationId,
                PurchaseOrderId = order.Id,
                ProductId = product.Id,
                Quantity = item.Quantity,
                UnitCost = product.Price
            });
        }

        order.Total = order.Lines.Sum(line => line.Quantity * line.UnitCost);
        dbContext.PurchaseOrders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(organizationId, order.Id, cancellationToken))!;
    }

    private void EnsureTenant(Guid organizationId)
    {
        if (!tenantContext.IsAuthenticated || tenantContext.OrganizationId != organizationId)
            throw new TenantAccessException();
    }

    private static PurchaseOrderResponse Map(PurchaseOrder order) =>
        new(order.Id, order.PurchaseOrderNumber, order.SupplierId, order.Supplier.Name, order.CreatedByUserId, order.User.Name, order.CreatedAtUtc, order.Status, order.PaymentStatus, order.Total,
            order.Lines.OrderBy(line => line.Product.Name)
                .Select(line => new PurchaseOrderLineResponse(line.Id, line.ProductId, line.Product.Name,
                    line.Quantity, line.UnitCost, line.Quantity * line.UnitCost))
                .ToArray());
}
