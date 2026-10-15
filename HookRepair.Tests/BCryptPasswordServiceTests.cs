using FluentAssertions;
using HookRepair.Modules.Auth.Infrastructure.Security;

namespace HookRepair.Tests;

public sealed class BCryptPasswordServiceTests
{
    private readonly BCryptPasswordService _service = new();

    [Fact]
    public void Hashes_password_without_retaining_plaintext()
    {
        const string password = "Strong-password-123!";

        var hash = _service.Hash(password);

        hash.Should().NotBe(password);
        _service.Verify(password, hash).Should().BeTrue();
        _service.Verify("Wrong-password-123!", hash).Should().BeFalse();
    }

    [Fact]
    public void Rejects_empty_or_malformed_hash()
    {
        _service.Verify("password", "").Should().BeFalse();
        _service.Verify("password", "not-a-bcrypt-hash").Should().BeFalse();
    }
}
