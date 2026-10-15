using HookRepair.Modules.Auth.Core.Entities;
using HookRepair.Modules.Crm.Core.Entities;

namespace HookRepair.Modules.Inventory.Core.Entities;

public enum PurchaseOrderStatus
{
    Pending = 0,
    Waiting = 1,
    InProgress = 2,
    ReceivedPartially = 3,
    Received = 4,
    InRma = 5,
    Canceled = 6
}

public enum PurchasePaymentStatus
{
    Unpaid = 0,
    Paid = 1
}

public sealed class PurchaseOrder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int PurchaseOrderNumber { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid SupplierId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Pending;
    public PurchasePaymentStatus PaymentStatus { get; set; } = PurchasePaymentStatus.Unpaid;
    public decimal Total { get; set; }

    public Supplier Supplier { get; set; }
    public User User { get; set; }

    public ICollection<PurchaseOrderLine> Lines { get; set; } = new List<PurchaseOrderLine>();
}
