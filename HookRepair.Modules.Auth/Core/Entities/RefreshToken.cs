namespace HookRepair.Modules.Auth.Core.Entities
{
    public class RefreshToken
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string TokenHash { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public User User { get; set; }
    }
}
