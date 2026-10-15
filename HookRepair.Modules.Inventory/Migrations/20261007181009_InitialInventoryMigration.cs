using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HookRepair.Modules.Inventory.Migrations
{
    /// <inheritdoc />
    public partial class InitialInventoryMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "inventory");

            migrationBuilder.CreateTable(
                name: "products",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_number = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    brand_id = table.Column<Guid>(type: "uuid", nullable: true),
                    upc = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false, computedColumnSql: "'4' || (CASE WHEN \"is_serialized\" THEN '1' ELSE '0' END) || LPAD(\"product_number\"::text, 10, '0')", stored: true),
                    image_url = table.Column<string>(type: "text", nullable: true),
                    condition = table.Column<string>(type: "text", nullable: true),
                    in_stock = table.Column<int>(type: "integer", nullable: false),
                    stock_warning = table.Column<int>(type: "integer", nullable: false),
                    minimum_reorder_point = table.Column<int>(type: "integer", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: true),
                    price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    general_sale_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    distributor_sale_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    is_serialized = table.Column<bool>(type: "boolean", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_products", x => x.id);
                    table.ForeignKey(
                        name: "fk_products_brand_brand_id",
                        column: x => x.brand_id,
                        principalSchema: "catalogs",
                        principalTable: "brands",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_products_category_category_id",
                        column: x => x.category_id,
                        principalSchema: "catalogs",
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_products_supplier_supplier_id",
                        column: x => x.supplier_id,
                        principalSchema: "crm",
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_orders",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_order_number = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    payment_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_orders", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "inventory_movements",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    previous_stock = table.Column<int>(type: "integer", nullable: false),
                    current_stock = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_movements", x => x.id);
                    table.ForeignKey(
                        name: "fk_inventory_movements_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "inventory",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_models",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    model_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_models", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_models_model_model_id",
                        column: x => x.model_id,
                        principalSchema: "catalogs",
                        principalTable: "models",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_product_models_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "inventory",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_serials",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    imei = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_serials", x => x.id);
                    table.ForeignKey(
                        name: "fk_product_serials_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "inventory",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_order_lines",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_order_lines", x => x.id);
                    table.CheckConstraint("ck_purchase_order_lines_quantity", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_purchase_order_lines_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "inventory",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_purchase_order_lines_purchase_orders_purchase_order_id",
                        column: x => x.purchase_order_id,
                        principalSchema: "inventory",
                        principalTable: "purchase_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_organization_id_product_id_created_at_u",
                schema: "inventory",
                table: "inventory_movements",
                columns: new[] { "organization_id", "product_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_product_id",
                schema: "inventory",
                table: "inventory_movements",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_models_model_id",
                schema: "inventory",
                table: "product_models",
                column: "model_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_models_product_id",
                schema: "inventory",
                table: "product_models",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_serials_imei_organization_id",
                schema: "inventory",
                table: "product_serials",
                columns: new[] { "imei", "organization_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_product_serials_product_id",
                schema: "inventory",
                table: "product_serials",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_brand_id",
                schema: "inventory",
                table: "products",
                column: "brand_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_category_id",
                schema: "inventory",
                table: "products",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_supplier_id",
                schema: "inventory",
                table: "products",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_upc_organization_id",
                schema: "inventory",
                table: "products",
                columns: new[] { "upc", "organization_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_order_lines_product_id",
                schema: "inventory",
                table: "purchase_order_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_order_lines_purchase_order_id_product_id",
                schema: "inventory",
                table: "purchase_order_lines",
                columns: new[] { "purchase_order_id", "product_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_organization_id_created_at_utc",
                schema: "inventory",
                table: "purchase_orders",
                columns: new[] { "organization_id", "created_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_movements",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "product_models",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "product_serials",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "purchase_order_lines",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "products",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "purchase_orders",
                schema: "inventory");
        }
    }
}
