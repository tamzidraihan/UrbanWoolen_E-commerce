using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UrbanWoolen.Migrations
{
    /// <inheritdoc />
    public partial class AddSizeChartsCrud : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChartType",
                table: "SizeCharts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "SizeCharts",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<decimal>(
                name: "Waist",
                table: "SizeChartItems",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Length",
                table: "SizeChartItems",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Chest",
                table: "SizeChartItems",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddColumn<decimal>(
                name: "FootLength",
                table: "SizeChartItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Hip",
                table: "SizeChartItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Inseam",
                table: "SizeChartItems",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "SizeChartItems",
                keyColumn: "Id",
                keyValue: 101,
                columns: new[] { "FootLength", "Hip", "Inseam" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "SizeChartItems",
                keyColumn: "Id",
                keyValue: 102,
                columns: new[] { "FootLength", "Hip", "Inseam" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "SizeChartItems",
                keyColumn: "Id",
                keyValue: 103,
                columns: new[] { "FootLength", "Hip", "Inseam" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "SizeChartItems",
                keyColumn: "Id",
                keyValue: 104,
                columns: new[] { "FootLength", "Hip", "Inseam" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "SizeChartItems",
                keyColumn: "Id",
                keyValue: 201,
                columns: new[] { "FootLength", "Hip", "Inseam" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "SizeChartItems",
                keyColumn: "Id",
                keyValue: 202,
                columns: new[] { "FootLength", "Hip", "Inseam" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "SizeChartItems",
                keyColumn: "Id",
                keyValue: 203,
                columns: new[] { "FootLength", "Hip", "Inseam" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "SizeChartItems",
                keyColumn: "Id",
                keyValue: 204,
                columns: new[] { "FootLength", "Hip", "Inseam" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "SizeCharts",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ChartType", "Title" },
                values: new object[] { 1, "Men Tops (BD)" });

            migrationBuilder.UpdateData(
                table: "SizeCharts",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ChartType", "Title" },
                values: new object[] { 1, "Women Tops (BD)" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChartType",
                table: "SizeCharts");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "SizeCharts");

            migrationBuilder.DropColumn(
                name: "FootLength",
                table: "SizeChartItems");

            migrationBuilder.DropColumn(
                name: "Hip",
                table: "SizeChartItems");

            migrationBuilder.DropColumn(
                name: "Inseam",
                table: "SizeChartItems");

            migrationBuilder.AlterColumn<decimal>(
                name: "Waist",
                table: "SizeChartItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Length",
                table: "SizeChartItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Chest",
                table: "SizeChartItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);
        }
    }
}
