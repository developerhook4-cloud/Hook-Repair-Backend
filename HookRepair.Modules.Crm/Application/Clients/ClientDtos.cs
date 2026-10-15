using HookRepair.Modules.Crm.Core.Entities;
using System.Text.Json.Serialization;

namespace HookRepair.Modules.Crm.Application.Clients;

public sealed record AddressResponse(
    string? Address,
    string? City,
    string? State,
    string? PostCode,
    string? Country);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class AddressRequest
{
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? PostCode { get; init; }
    public string? Country { get; init; }
}

public sealed record ClientResponse(
    Guid Id,
    string Name,
    CustomerType CustomerType,
    string Phone1,
    string? Phone2,
    string? LegalName,
    string? Email,
    AddressResponse? Address,
    string? TitularName,
    string[]? AuthorizedPeople,
    string? Rfc,
    string? TaxRegime,
    string? ExpenseType,
    bool Active);

public sealed record ClientMutationResponse(Guid Id, string Message);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class CreateClientRequest
{
    public string? Name { get; init; }
    public CustomerType CustomerType { get; init; } = CustomerType.Public;
    public string? Phone1 { get; init; }
    public string? Phone2 { get; init; }
    public string? LegalName { get; init; }
    public string? Email { get; init; }
    public AddressRequest? Address { get; init; }
    public string? TitularName { get; init; }
    public string[]? AuthorizedPeople { get; init; }
    public string? Rfc { get; init; }
    public string? TaxRegime { get; init; }
    public string? ExpenseType { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class UpdateClientRequest : CreateClientRequest;
