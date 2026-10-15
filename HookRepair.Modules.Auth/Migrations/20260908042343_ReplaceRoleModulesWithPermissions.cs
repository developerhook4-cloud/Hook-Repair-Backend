using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HookRepair.Modules.Auth.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceRoleModulesWithPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "permissions",
                table: "roles",
                type: "text[]",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE roles AS role
                SET permissions = COALESCE(
                    (
                        SELECT array_agg(role_module.module ORDER BY role_module.module)
                        FROM role_modules AS role_module
                        WHERE role_module.role_id = role.id
                    ),
                    ARRAY[]::text[]
                );
                """);

            migrationBuilder.AlterColumn<string[]>(
                name: "permissions",
                table: "roles",
                type: "text[]",
                nullable: false,
                oldClrType: typeof(string[]),
                oldType: "text[]",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_roles_permissions",
                table: "roles",
                sql: "permissions <@ ARRAY['auth','third_parties','inventory','purchases','devices','service','pos','notifications','reports','expenses','organizations','configuration']::text[]");

            migrationBuilder.DropTable(
                name: "role_modules");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "role_modules",
                columns: table => new
                {
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    module = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_modules", x => new { x.role_id, x.module });
                    table.CheckConstraint("ck_role_modules_module", "module IN ('auth','third_parties','inventory','purchases','devices','service','pos','notifications','reports','expenses','organizations','configuration')");
                    table.ForeignKey(
                        name: "fk_role_modules_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO role_modules (role_id, module)
                SELECT role.id, permission.module
                FROM roles AS role
                CROSS JOIN LATERAL unnest(role.permissions) AS permission(module);
                """);

            migrationBuilder.DropCheckConstraint(
                name: "ck_roles_permissions",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "permissions",
                table: "roles");
        }
    }
}
