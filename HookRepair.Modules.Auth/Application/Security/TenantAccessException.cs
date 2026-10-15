namespace HookRepair.Modules.Auth.Application.Security;

public sealed class TenantAccessException()
    : Exception("The authenticated user does not belong to the requested organization.");
