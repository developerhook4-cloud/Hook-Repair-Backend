namespace HookRepair.Modules.Inventory.Core.Entities;

public sealed class GoodsReceiptLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Guid GoodsReceiptId { get; set; }
    public Guid PurchaseOrderLineId { get; set; }
    public Guid ProductId { get; set; }
    public int OrderedQuantity { get; set; }
    public int ReceivedQuantity { get; set; }

    public GoodsReceipt GoodsReceipt { get; set; } = null!;
    public PurchaseOrderLine PurchaseOrderLine { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
