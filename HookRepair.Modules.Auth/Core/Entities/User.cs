namespace HookRepair.Modules.Auth.Core.Entities
{
    public class User
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public Guid OrganizationId { get; set; }

        public Guid RoleId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public Organization Organization { get; set; }
        public Role Role { get; set; }
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}
