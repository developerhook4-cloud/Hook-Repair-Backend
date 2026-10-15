using System.ComponentModel.DataAnnotations;
using System.Reflection;
using FluentAssertions;
using HookRepair.Modules.Auth.Application.Auth;

namespace HookRepair.Tests;

public sealed class AuthRequestValidationTests
{
    [Fact]
    public void Login_validation_attributes_are_declared_on_record_parameters()
    {
        var constructor = typeof(LoginRequest).GetConstructors().Single();
        var parameters = constructor.GetParameters();

        parameters.Should().HaveCount(2);
        parameters.SelectMany(parameter => parameter.GetCustomAttributes<ValidationAttribute>())
            .Should().NotBeEmpty();
        typeof(LoginRequest).GetProperties()
            .SelectMany(property => property.GetCustomAttributes<ValidationAttribute>())
            .Should().BeEmpty();
    }
}
