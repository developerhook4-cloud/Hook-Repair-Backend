using HookRepair.Modules.Catalogs.Core.Entities;
using HookRepair.Modules.Crm.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HookRepair.Modules.Inventory.Core.Entities
{
    public class Product
    {
        public Guid Id { get; set; }
        // numero que se genera automatcamente para cada producto, se utiliza para generar el codigo de barras/UPC
        public int ProductNumber { get; set; }
        public Guid OrganizationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid? CategoryId { get; set; }
        public Guid? BrandId {  get; set; }
        public string UPC { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? Condition { get; set; }
        public int InStock { get; set; }
        public int StockWarning {  get; set; }
        public int MinimumReorderPoint { get; set; }
        public Guid? SupplierId { get; set; }
        public decimal Price { get; set; }
        public decimal GeneralSalePrice { get; set; }
        public decimal DistributorSalePrice { get; set; }
        public bool IsSerialized { get; set; }
        public bool Active { get; set; }

        public Category? Category { get; set; }
        public Brand? Brand { get; set; }
        public Supplier? Supplier { get; set; }

        public ICollection<ProductModel> Models { get; set; } = new List<ProductModel>();
        public ICollection<ProductSerial> Serials { get; set; } = new List<ProductSerial>();
        public ICollection<InventoryMovement> Movements { get; set; } =
            new List<InventoryMovement>();

    }


}
