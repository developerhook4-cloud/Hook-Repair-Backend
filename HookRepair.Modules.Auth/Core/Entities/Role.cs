namespace HookRepair.Modules.Auth.Core.Entities
{
    public class Role
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public Guid OrganizationId { get; init; }
        public bool Active { get; set; } = true;
        public string Name { get; set; } = string.Empty;
        public string[] Permissions { get; set; } = [];
        public Organization Organization { get; set; } = null!;
    }
}
