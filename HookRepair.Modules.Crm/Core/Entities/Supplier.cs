using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HookRepair.Modules.Crm.Core.Entities
{
    public class Supplier
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public Guid OrganizationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone1 { get; set; } = string.Empty;
        public string? Phone2 { get; set; }
        public string Email { get; set; } = string.Empty;
        public bool Active { get; set; } = true;
    }
}
