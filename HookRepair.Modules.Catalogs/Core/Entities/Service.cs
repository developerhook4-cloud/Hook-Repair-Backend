using HookRepair.Modules.Auth.Core.Entities;

namespace HookRepair.Modules.Catalogs.Core.Entities;

public sealed class Service
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OrganizationId { get; init; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public Guid? BrandId { get; set; }
    public Guid? ModelId {  get; set; }
    public decimal RetailPrice { get; set; }
    public decimal PublicPrice { get; set; }
    public decimal DistributorPrice { get; set; }
    public bool Active { get; set; } = true;

    public Category? Category { get; set; }
    public Brand? Brand { get; set; }
    public Model? Model { get; set; }
}
