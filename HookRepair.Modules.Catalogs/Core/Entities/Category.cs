namespace HookRepair.Modules.Catalogs.Core.Entities;

public sealed class Category
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OrganizationId { get; init; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
}
