using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class FixProductRowVersionColumnType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // نشيل العمود الغلط (varbinary(max) NOT NULL عادي)
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Products");

            // ونضيفه تاني كـ ROWVERSION حقيقي - SQL Server هيعمّره تلقائيًا في كل INSERT/UPDATE
            migrationBuilder.Sql("ALTER TABLE Products ADD RowVersion ROWVERSION;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Products");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Products",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);
        }
    }
}