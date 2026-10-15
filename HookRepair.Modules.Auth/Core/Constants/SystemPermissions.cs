using System.Reflection;

namespace HookRepair.Modules.Auth.Core.Constants
{
    public static class SystemPermissions
    {
        public const string ViewOrganizations = "Organizations.View";
        public const string UpdateOrganizations = "Organizations.Update";
        public const string DeleteOrganizations = "Organizations.Delete";

        public const string ViewUsers = "Users.View";
        public const string UpdateUsers = "Users.Update";
        public const string DeleteUsers = "Users.Delete";

        public const string ViewRoles = "Roles.View";
        public const string UpdateRoles = "Roles.Update";
        public const string DeleteRoles = "Roles.Delete";

        public const string ViewPos = "Pos.View";
        public const string UpdatePos = "Pos.Update";
        public const string DeletePos = "Pos.Delete";

        public const string ViewRepairs = "Repairs.View";
        public const string UpdateRepairs = "Repairs.Update";
        public const string DeleteRepairs = "Repairs.Delete";

        public const string ViewInventory = "Inventory.View";
        public const string UpdateInventory = "Inventory.Update";
        public const string DeleteInventory = "Inventory.Delete";

        public const string ViewCustomers = "Customers.View";
        public const string UpdateCustomers = "Customers.Update";
        public const string DeleteCustomers = "Customers.Delete";

        public const string ViewSuppliers = "Suppliers.View";
        public const string UpdateSuppliers = "Suppliers.Update";
        public const string DeleteSuppliers = "Suppliers.Delete";

        public const string ViewCatalogs = "Catalogs.View";
        public const string UpdateCatalogs = "Catalogs.Update";
        public const string DeleteCatalogs = "Catalogs.Delete";

        public const string ViewDevices = "Devices.View";
        public const string UpdateDevices = "Devices.Update";
        public const string DeleteDevices = "Devices.Delete";

        public const string ViewServices = "Services.View";
        public const string UpdateServices = "Services.Update";
        public const string DeleteServices = "Services.Delete";

        public const string ViewRepairStatus = "RepairStatus.View";
        public const string UpdateRepairStatus = "RepairStatus.Update";
        public const string DeleteRepairStatus = "RepairStatus.Delete";

        public const string ViewReports = "Reports.View";
        public const string UpdateReports = "Reports.Update";
        public const string DeleteReports = "Reports.Delete";

        public static readonly HashSet<string> ValidPermissions = new(
            typeof(SystemPermissions)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
                .Select(f => (string)f.GetRawConstantValue()!),
            StringComparer.OrdinalIgnoreCase
        );
    }
}
