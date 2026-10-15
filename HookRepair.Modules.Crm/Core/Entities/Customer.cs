using HookRepair.Shared.Core.ValueObjects;

namespace HookRepair.Modules.Crm.Core.Entities
{
    public enum CustomerType
    {
        Public,
        Distributor
    }
    public class Customer
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public Guid OrganizationId { get; set; }
        public CustomerType CustomerType { get; set; } = CustomerType.Public;
        public string Name { get; set; } = string.Empty;
        public string Phone1 { get; set; } = string.Empty;
        public string? Phone2 { get; set; }
        public string? LegalName { get; set; }
        public string? Email { get; set; }
        public FullAddress? Address { get; set; }
        public string? TitularName { get; set; }
        public string[]? AuthorizedPeople { get; set; }
        public string? Rfc { get; set; }
        public string? TaxRegime { get; set; }
        public string? ExpenseType { get; set; }
        public bool Active { get; set; } = true;
    }
}
