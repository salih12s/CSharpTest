using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TedarikCore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforceUppercaseSku : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "ck_products_sku_uppercase",
                table: "products",
                sql: "sku = upper(sku)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_products_sku_uppercase",
                table: "products");
        }
    }
}
