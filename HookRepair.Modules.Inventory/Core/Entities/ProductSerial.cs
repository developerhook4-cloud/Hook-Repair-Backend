using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HookRepair.Modules.Inventory.Core.Entities
{
    public class ProductSerial
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid ProductId { get; set; }
        public string IMEI { get; set; } = string.Empty;
        public string Status { get; set; } = "Disponible";

        public Product Product { get; set; }
    }
}
