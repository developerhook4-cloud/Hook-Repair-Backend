using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HookRepair.Modules.Crm.Migrations
{
    /// <inheritdoc />
    public partial class CreateCrmDbContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "crm");

            migrationBuilder.CreateTable(
                name: "customers",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_type = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    phone1 = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    phone2 = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    legal_name = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "text", nullable: true),
                    address_address = table.Column<string>(type: "text", nullable: true),
                    address_city = table.Column<string>(type: "text", nullable: true),
                    address_state = table.Column<string>(type: "text", nullable: true),
                    address_post_code = table.Column<string>(type: "text", nullable: true),
                    address_country = table.Column<string>(type: "text", nullable: true),
                    titular_name = table.Column<string>(type: "text", nullable: true),
                    authorized_people = table.Column<string[]>(type: "text[]", nullable: true),
                    rfc = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                    tax_regime = table.Column<string>(type: "text", nullable: true),
                    expense_type = table.Column<string>(type: "text", nullable: true),
                    active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "suppliers",
                schema: "crm",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    phone1 = table.Column<string>(type: "text", nullable: false),
                    phone2 = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "text", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_suppliers", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customers",
                schema: "crm");

            migrationBuilder.DropTable(
                name: "suppliers",
                schema: "crm");
        }
    }
}
