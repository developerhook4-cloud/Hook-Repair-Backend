using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HookRepair.Api.Security;

internal static class DbUpdateConflict
{
    public static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.GetBaseException() is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
