using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HookRepair.Modules.Inventory.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAndSupplierToPurchaseOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "organization",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    website = table.Column<string>(type: "text", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    phone1 = table.Column<string>(type: "text", nullable: true),
                    phone2 = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organization", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "refresh_token",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_token", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_token_user_user_id",
                        column: x => x.user_id,
                        principalSchema: "public",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "role",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    permissions = table.Column<string[]>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role", x => x.id);
                    table.ForeignKey(
                        name: "fk_role_organization_organization_id",
                        column: x => x.organization_id,
                        principalSchema: "inventory",
                        principalTable: "organization",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_created_by_user_id",
                schema: "inventory",
                table: "purchase_orders",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_supplier_id",
                schema: "inventory",
                table: "purchase_orders",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_token_user_id",
                schema: "inventory",
                table: "refresh_token",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_organization_id",
                schema: "inventory",
                table: "role",
                column: "organization_id");

            migrationBuilder.AddForeignKey(
                name: "fk_purchase_orders_supplier_supplier_id",
                schema: "inventory",
                table: "purchase_orders",
                column: "supplier_id",
                principalSchema: "crm",
                principalTable: "suppliers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_purchase_orders_user_created_by_user_id",
                schema: "inventory",
                table: "purchase_orders",
                column: "created_by_user_id",
                principalSchema: "public",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_purchase_orders_supplier_supplier_id",
                schema: "inventory",
                table: "purchase_orders");

            migrationBuilder.DropForeignKey(
                name: "fk_purchase_orders_user_created_by_user_id",
                schema: "inventory",
                table: "purchase_orders");

            migrationBuilder.DropTable(
                name: "refresh_token",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "role",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "organization",
                schema: "inventory");

            migrationBuilder.DropIndex(
                name: "ix_purchase_orders_created_by_user_id",
                schema: "inventory",
                table: "purchase_orders");

            migrationBuilder.DropIndex(
                name: "ix_purchase_orders_supplier_id",
                schema: "inventory",
                table: "purchase_orders");
        }
    }
}
