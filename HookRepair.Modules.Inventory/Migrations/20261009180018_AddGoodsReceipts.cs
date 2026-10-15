using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HookRepair.Modules.Inventory.Migrations
{
    /// <inheritdoc />
    public partial class AddGoodsReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "goods_receipts",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    goods_receipt_number = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    received_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_goods_receipts", x => x.id);
                    table.ForeignKey(
                        name: "fk_goods_receipts_purchase_orders_purchase_order_id",
                        column: x => x.purchase_order_id,
                        principalSchema: "inventory",
                        principalTable: "purchase_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "goods_receipt_lines",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    goods_receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_order_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ordered_quantity = table.Column<int>(type: "integer", nullable: false),
                    received_quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_goods_receipt_lines", x => x.id);
                    table.CheckConstraint("ck_goods_receipt_lines_received_quantity", "received_quantity >= 0");
                    table.ForeignKey(
                        name: "fk_goods_receipt_lines_goods_receipts_goods_receipt_id",
                        column: x => x.goods_receipt_id,
                        principalSchema: "inventory",
                        principalTable: "goods_receipts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_goods_receipt_lines_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "inventory",
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_goods_receipt_lines_purchase_order_lines_purchase_order_lin",
                        column: x => x.purchase_order_line_id,
                        principalSchema: "inventory",
                        principalTable: "purchase_order_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_goods_receipt_lines_goods_receipt_id_purchase_order_line_id",
                schema: "inventory",
                table: "goods_receipt_lines",
                columns: new[] { "goods_receipt_id", "purchase_order_line_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_goods_receipt_lines_product_id",
                schema: "inventory",
                table: "goods_receipt_lines",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_goods_receipt_lines_purchase_order_line_id",
                schema: "inventory",
                table: "goods_receipt_lines",
                column: "purchase_order_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_goods_receipts_organization_id_created_at_utc",
                schema: "inventory",
                table: "goods_receipts",
                columns: new[] { "organization_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_goods_receipts_purchase_order_id",
                schema: "inventory",
                table: "goods_receipts",
                column: "purchase_order_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "goods_receipt_lines",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "goods_receipts",
                schema: "inventory");
        }
    }
}
