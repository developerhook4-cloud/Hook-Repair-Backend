namespace HookRepair.Modules.Auth.Application.Security;

public interface ITenantContext
{
    Guid OrganizationId { get; }
    bool IsAuthenticated { get; }
    bool IsPlatform { get; }
    void Set(Guid organizationId, bool isPlatform = false);
}
