using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HookRepair.Modules.Catalogs.Migrations
{
    /// <inheritdoc />
    public partial class AddIconFieldForCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "icon",
                schema: "catalogs",
                table: "categories",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_services_brand_id",
                schema: "catalogs",
                table: "services",
                column: "brand_id");

            migrationBuilder.CreateIndex(
                name: "ix_services_category_id",
                schema: "catalogs",
                table: "services",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_services_model_id",
                schema: "catalogs",
                table: "services",
                column: "model_id");

            migrationBuilder.AddForeignKey(
                name: "fk_services_brands_brand_id",
                schema: "catalogs",
                table: "services",
                column: "brand_id",
                principalSchema: "catalogs",
                principalTable: "brands",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_services_categories_category_id",
                schema: "catalogs",
                table: "services",
                column: "category_id",
                principalSchema: "catalogs",
                principalTable: "categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_services_models_model_id",
                schema: "catalogs",
                table: "services",
                column: "model_id",
                principalSchema: "catalogs",
                principalTable: "models",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_services_brands_brand_id",
                schema: "catalogs",
                table: "services");

            migrationBuilder.DropForeignKey(
                name: "fk_services_categories_category_id",
                schema: "catalogs",
                table: "services");

            migrationBuilder.DropForeignKey(
                name: "fk_services_models_model_id",
                schema: "catalogs",
                table: "services");

            migrationBuilder.DropIndex(
                name: "ix_services_brand_id",
                schema: "catalogs",
                table: "services");

            migrationBuilder.DropIndex(
                name: "ix_services_category_id",
                schema: "catalogs",
                table: "services");

            migrationBuilder.DropIndex(
                name: "ix_services_model_id",
                schema: "catalogs",
                table: "services");

            migrationBuilder.DropColumn(
                name: "icon",
                schema: "catalogs",
                table: "categories");
        }
    }
}
