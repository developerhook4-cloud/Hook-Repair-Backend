using HookRepair.Shared.Core.ValueObjects;
namespace HookRepair.Modules.Auth.Core.Entities
{
    public class Organization
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public required string Name { get; set; }
        public required string Email { get; set; }
        public string? Website { get; set; }
        public bool Active { get; set; } = true;
        public string? Phone1 { get; set; }
        public string? Phone2 { get; set; }
        public FullAddress? Address { get; set; }
    }
}
