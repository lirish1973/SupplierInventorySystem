using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SupplierInventorySystem.Migrations
{
    /// <inheritdoc />
    public partial class AddStockAdjustmentLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stock_adjustment_logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    product_id = table.Column<int>(type: "int", nullable: false),
                    quantity_change = table.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    quantity_before = table.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    quantity_after = table.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    reason = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    adjusted_by = table.Column<int>(type: "int", nullable: true),
                    adjusted_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_adjustment_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_stock_adjustment_logs_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_stock_adjustment_logs_users_adjusted_by",
                        column: x => x.adjusted_by,
                        principalTable: "users",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_stock_adjustment_logs_adjusted_by",
                table: "stock_adjustment_logs",
                column: "adjusted_by");

            migrationBuilder.CreateIndex(
                name: "IX_stock_adjustment_logs_product_id",
                table: "stock_adjustment_logs",
                column: "product_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stock_adjustment_logs");
        }
    }
}
