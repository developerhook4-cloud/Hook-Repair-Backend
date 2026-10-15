using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HookRepair.Modules.Auth.Migrations
{
    /// <inheritdoc />
    public partial class RequireOrganizationIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM users WHERE organization_id IS NULL)
                       OR EXISTS (SELECT 1 FROM roles WHERE organization_id IS NULL) THEN
                        RAISE EXCEPTION 'Cannot require organization_id: users or roles contain NULL values.';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropForeignKey(
                name: "fk_roles_organizations_organization_id",
                table: "roles");

            migrationBuilder.DropForeignKey(
                name: "fk_users_organizations_organization_id",
                table: "users");

            migrationBuilder.AlterColumn<Guid>(
                name: "organization_id",
                table: "users",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "organization_id",
                table: "roles",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_roles_organizations_organization_id",
                table: "roles",
                column: "organization_id",
                principalTable: "organizations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_users_organizations_organization_id",
                table: "users",
                column: "organization_id",
                principalTable: "organizations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_roles_organizations_organization_id",
                table: "roles");

            migrationBuilder.DropForeignKey(
                name: "fk_users_organizations_organization_id",
                table: "users");

            migrationBuilder.AlterColumn<Guid>(
                name: "organization_id",
                table: "users",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "organization_id",
                table: "roles",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "fk_roles_organizations_organization_id",
                table: "roles",
                column: "organization_id",
                principalTable: "organizations",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_users_organizations_organization_id",
                table: "users",
                column: "organization_id",
                principalTable: "organizations",
                principalColumn: "id");
        }
    }
}
