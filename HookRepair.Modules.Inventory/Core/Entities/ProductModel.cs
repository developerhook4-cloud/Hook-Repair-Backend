using HookRepair.Modules.Catalogs.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HookRepair.Modules.Inventory.Core.Entities
{
    public class ProductModel
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid ProductId { get; set; }
        public Guid ModelId { get; set; }

        public Model Model { get; set; }
        public Product Product { get; set; }
    }
}
