using System.Text.RegularExpressions;
using HookRepair.Modules.Crm.Application.Clients;
using HookRepair.Modules.Crm.Application.Suppliers;
using HookRepair.Modules.Crm.Core.Entities;

namespace HookRepair.Modules.Crm.Application.ThirdParties;

public static partial class ThirdPartyRequestValidator
{
    public static string? Validate(CreateClientRequest? request) => ValidateClient(request);
    public static string? Validate(UpdateClientRequest? request) => ValidateClient(request);
    public static string? Validate(CreateSupplierRequest? request) => ValidateSupplier(request);
    public static string? Validate(UpdateSupplierRequest? request) => ValidateSupplier(request);

    private static string? ValidateClient(CreateClientRequest? request)
    {
        if (request is null) return "request is required.";
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 200)
            return "name must contain between 1 and 200 characters.";
        if (!Enum.IsDefined(request.CustomerType))
            return "customerType is invalid.";
        if (string.IsNullOrWhiteSpace(request.Phone1) || request.Phone1.Trim().Length > 20)
            return "phone1 must contain between 1 and 20 characters.";
        if (request.Phone2?.Trim().Length > 20)
            return "phone2 cannot contain more than 20 characters.";
        if (!string.IsNullOrWhiteSpace(request.Email) &&
            (request.Email.Trim().Length > 254 || !EmailPattern().IsMatch(request.Email.Trim())))
            return "email must be a valid email address with at most 254 characters.";
        if (request.Rfc?.Trim().Length > 13)
            return "rfc cannot contain more than 13 characters.";
        return ValidateAddress(request.Address);
    }

    private static string? ValidateSupplier(CreateSupplierRequest? request)
    {
        if (request is null) return "request is required.";
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 200)
            return "name must contain between 1 and 200 characters.";
        if (string.IsNullOrWhiteSpace(request.Phone1) || request.Phone1.Trim().Length > 20)
            return "phone1 must contain between 1 and 20 characters.";
        if (request.Phone2?.Trim().Length > 20)
            return "phone2 cannot contain more than 20 characters.";
        if (string.IsNullOrWhiteSpace(request.Email))
            return "email is required.";
        if (!string.IsNullOrWhiteSpace(request.Email) &&
            (request.Email.Trim().Length > 254 || !EmailPattern().IsMatch(request.Email.Trim())))
            return "email must be a valid email address with at most 254 characters.";
        return null;
    }

    private static string? ValidateAddress(AddressRequest? address)
    {
        if (address is null) return null;
        return new[]
        {
            address.Address, address.City, address.State, address.PostCode, address.Country
        }.Any(value => value?.Trim().Length > 500)
            ? "address fields cannot contain more than 500 characters."
            : null;
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();
}
