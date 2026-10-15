using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HookRepair.Modules.Auth.Migrations;

public partial class AlignAuthSecurity : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("fk_roles_organizations_organization_id", "roles");
        migrationBuilder.DropForeignKey("fk_users_organizations_organization_id", "users");

        migrationBuilder.AddColumn<DateTime>("revoked_at", "refresh_tokens", type: "timestamp with time zone", nullable: true);
        migrationBuilder.Sql("UPDATE refresh_tokens SET revoked_at = created_at WHERE revoked = TRUE");
        migrationBuilder.DropColumn("revoked", "refresh_tokens");

        migrationBuilder.RenameColumn("password", "users", "password_hash");
        migrationBuilder.RenameColumn("token", "refresh_tokens", "token_hash");
        migrationBuilder.RenameIndex(name: "ix_refresh_tokens_token", table: "refresh_tokens", newName: "ix_refresh_tokens_token_hash");

        migrationBuilder.AlterColumn<Guid>("organization_id", "users", type: "uuid", nullable: true, oldClrType: typeof(Guid), oldType: "uuid");
        migrationBuilder.AlterColumn<string>("name", "users", type: "character varying(150)", maxLength: 150, nullable: false, oldClrType: typeof(string), oldType: "text");
        migrationBuilder.AlterColumn<Guid>("organization_id", "roles", type: "uuid", nullable: true, oldClrType: typeof(Guid), oldType: "uuid");
        migrationBuilder.AlterColumn<string>("name", "organizations", type: "character varying(200)", maxLength: 200, nullable: false, oldClrType: typeof(string), oldType: "text");

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
                table.ForeignKey("fk_role_modules_roles_role_id", x => x.role_id, "roles", "id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.AddForeignKey(name: "fk_roles_organizations_organization_id", table: "roles", column: "organization_id", principalTable: "organizations", principalColumn: "id");
        migrationBuilder.AddForeignKey(name: "fk_users_organizations_organization_id", table: "users", column: "organization_id", principalTable: "organizations", principalColumn: "id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("fk_roles_organizations_organization_id", "roles");
        migrationBuilder.DropForeignKey("fk_users_organizations_organization_id", "users");
        migrationBuilder.DropTable("role_modules");

        migrationBuilder.AddColumn<bool>("revoked", "refresh_tokens", type: "boolean", nullable: false, defaultValue: false);
        migrationBuilder.Sql("UPDATE refresh_tokens SET revoked = TRUE WHERE revoked_at IS NOT NULL");
        migrationBuilder.DropColumn("revoked_at", "refresh_tokens");
        migrationBuilder.RenameColumn("password_hash", "users", "password");
        migrationBuilder.RenameColumn("token_hash", "refresh_tokens", "token");
        migrationBuilder.RenameIndex(name: "ix_refresh_tokens_token_hash", table: "refresh_tokens", newName: "ix_refresh_tokens_token");

        migrationBuilder.AlterColumn<Guid>("organization_id", "users", type: "uuid", nullable: false, oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);
        migrationBuilder.AlterColumn<string>("name", "users", type: "text", nullable: false, oldClrType: typeof(string), oldType: "character varying(150)", oldMaxLength: 150);
        migrationBuilder.AlterColumn<Guid>("organization_id", "roles", type: "uuid", nullable: false, oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);
        migrationBuilder.AlterColumn<string>("name", "organizations", type: "text", nullable: false, oldClrType: typeof(string), oldType: "character varying(200)", oldMaxLength: 200);

        migrationBuilder.AddForeignKey(name: "fk_roles_organizations_organization_id", table: "roles", column: "organization_id", principalTable: "organizations", principalColumn: "id", onDelete: ReferentialAction.Cascade);
        migrationBuilder.AddForeignKey(name: "fk_users_organizations_organization_id", table: "users", column: "organization_id", principalTable: "organizations", principalColumn: "id", onDelete: ReferentialAction.Cascade);
    }
}
