using System.Text.Json.Serialization;

namespace HookRepair.Modules.Crm.Application.Suppliers;

public sealed record SupplierResponse(
    Guid Id,
    string Name,
    string Phone1,
    string? Phone2,
    string Email,
    bool Active);

public sealed record SupplierMutationResponse(Guid Id, string Message);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class CreateSupplierRequest
{
    public string? Name { get; init; }
    public string? Phone1 { get; init; }
    public string? Phone2 { get; init; }
    public string? Email { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class UpdateSupplierRequest : CreateSupplierRequest;
