namespace HookRepair.Modules.Inventory.Core.Entities;

public enum InventoryMovementType
{
    Entry,
    Exit,
    Adjustment
}

public sealed class InventoryMovement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Guid ProductId { get; set; }
    public InventoryMovementType Type { get; set; }
    public int Quantity { get; set; }
    public int PreviousStock { get; set; }
    public int CurrentStock { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Product Product { get; set; } = null!;
}