using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HookRepair.Api.Models.Organizations;

public sealed class SaveOrganizationRequest
{
    [Required, StringLength(200)]
    public string Name { get; init; } = string.Empty;

    [Required, EmailAddress, StringLength(320)]
    public string Email { get; init; } = string.Empty;

    [StringLength(500)]
    public string Website { get; init; } = string.Empty;

    [StringLength(30)]
    public string Phone1 { get; init; } = string.Empty;

    [StringLength(30)]
    public string Phone2 { get; init; } = string.Empty;

    public bool Active { get; init; } = true;

    public AddressRequest Address { get; init; } = new();
}

public sealed class AddressRequest
{
    [StringLength(300)]
    public string Address { get; init; } = string.Empty;

    [StringLength(100)]
    public string City { get; init; } = string.Empty;

    [StringLength(100)]
    public string State { get; init; } = string.Empty;

    [StringLength(20)]
    public string PostCode { get; init; } = string.Empty;

    [StringLength(100)]
    public string Country { get; init; } = string.Empty;
}

public sealed class AddUserToOrganizationRequest
{
    [Required, StringLength(300)]
    public string Name { get; init; } = string.Empty;
    [Required, StringLength(320), EmailAddress]
    public string Email { get; init; } = string.Empty;
    [Required, StringLength(100)]
    public string Password { get; init; } = string.Empty;
    [Required]
    public Guid RoleId { get; init; } = Guid.Empty;
}

public sealed class UpdateUserInOrganizationRequest
{
    [StringLength(300)]
    public string Name { get; init; } = string.Empty;
    [StringLength(320), EmailAddress]
    public string Email { get; init; } = string.Empty;
    [StringLength(100)]
    public string Password { get; init; } = string.Empty;
    public Guid RoleId { get; init; } = Guid.Empty;
}

public sealed record UserResponse
(
    Guid Id,
    string Name,
    string Email,
    string RoleName,
    bool isActive
);

public sealed record OrganizationResponse(
    Guid Id,
    string Name,
    string Email,
    string Website,
    bool Active,
    string Phone1,
    string Phone2,
    AddressResponse Address);

public sealed record AddressResponse(
    string Address,
    string City,
    string State,
    string PostCode,
    string Country);