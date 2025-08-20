using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace UrbanWoolen.Migrations
{
    /// <inheritdoc />
    public partial class AddSizeCharts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SizeCharts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Region = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SizeCharts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SizeChartItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SizeChartId = table.Column<int>(type: "int", nullable: false),
                    Size = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Chest = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Waist = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Length = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SizeChartItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SizeChartItems_SizeCharts_SizeChartId",
                        column: x => x.SizeChartId,
                        principalTable: "SizeCharts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "SizeCharts",
                columns: new[] { "Id", "Category", "Region", "Unit" },
                values: new object[,]
                {
                    { 1, 0, "BD", "cm" },
                    { 2, 1, "BD", "cm" }
                });

            migrationBuilder.InsertData(
                table: "SizeChartItems",
                columns: new[] { "Id", "Chest", "Length", "Size", "SizeChartId", "Waist" },
                values: new object[,]
                {
                    { 101, 92m, 67m, "S", 1, 78m },
                    { 102, 98m, 69m, "M", 1, 84m },
                    { 103, 104m, 71m, "L", 1, 90m },
                    { 104, 110m, 73m, "XL", 1, 96m },
                    { 201, 84m, 62m, "S", 2, 66m },
                    { 202, 90m, 64m, "M", 2, 72m },
                    { 203, 96m, 66m, "L", 2, 78m },
                    { 204, 102m, 68m, "XL", 2, 84m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_SizeChartItems_SizeChartId",
                table: "SizeChartItems",
                column: "SizeChartId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SizeChartItems");

            migrationBuilder.DropTable(
                name: "SizeCharts");
        }
    }
}
