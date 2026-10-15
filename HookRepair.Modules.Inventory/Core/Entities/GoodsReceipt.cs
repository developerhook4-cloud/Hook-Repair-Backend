namespace HookRepair.Modules.Inventory.Core.Entities;

public sealed class GoodsReceipt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int GoodsReceiptNumber { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public Guid ReceivedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }

    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public ICollection<GoodsReceiptLine> Lines { get; set; } = new List<GoodsReceiptLine>();
}
