namespace HookRepair.Modules.Catalogs.Core.Entities;

public sealed class Model
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OrganizationId { get; init; }
    public Guid BrandId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
    public Brand Brand { get; set; } = null!;

    public string BrandName => Brand?.Name ?? string.Empty;
}
