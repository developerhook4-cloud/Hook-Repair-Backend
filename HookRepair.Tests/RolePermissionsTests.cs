using FluentAssertions;
using HookRepair.Modules.Auth.Core.Entities;
using HookRepair.Modules.Auth.Infrastructure;
using HookRepair.Modules.Auth.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace HookRepair.Tests;

public sealed class RolePermissionsTests
{
    [Fact]
    public void Role_permissions_are_string_array_without_join_entity()
    {
        typeof(Role).GetProperty(nameof(Role.Permissions))!.PropertyType.Should().Be(typeof(string[]));

        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new AuthDbContext(options, new TenantContext());

        db.Model.GetEntityTypes().Should().NotContain(entity => entity.ClrType.Name == "RoleModule");
    }
}
