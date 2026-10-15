using HookRepair.Modules.Auth.Application.Security;

namespace HookRepair.Modules.Auth.Infrastructure.Security;

public sealed class TenantContext : ITenantContext
{
    public Guid OrganizationId { get; private set; }
    public bool IsAuthenticated { get; private set; }
    public bool IsPlatform { get; private set; }

    public void Set(Guid organizationId, bool isPlatform = false)
    {
        if (organizationId == Guid.Empty)
            throw new InvalidOperationException("Tenant organization is required.");

        OrganizationId = organizationId;
        IsPlatform = isPlatform;
        IsAuthenticated = true;
    }
}
