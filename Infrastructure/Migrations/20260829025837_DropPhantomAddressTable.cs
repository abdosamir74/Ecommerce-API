using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropPhantomAddressTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
        IF OBJECT_ID('dbo.Address', 'U') IS NOT NULL
            DROP TABLE dbo.Address;
    ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // مش محتاجين نرجعها
        }
    }
}
