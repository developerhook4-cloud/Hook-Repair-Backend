using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HookRepair.Modules.Auth.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRolePermissionConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_roles_permissions",
                table: "roles");

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "organizations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "organizations",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AddCheckConstraint(
                name: "ck_roles_permissions",
                table: "roles",
                sql: "permissions <@ ARRAY['auth','third_parties','inventory','purchases','devices','service','pos','notifications','reports','expenses','organizations','configuration']::text[]");
        }
    }
}
