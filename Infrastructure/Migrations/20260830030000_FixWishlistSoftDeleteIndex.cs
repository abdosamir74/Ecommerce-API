using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class FixWishlistSoftDeleteIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WishlistItems_AppUserId_ProductId",
                table: "WishlistItems");

            migrationBuilder.CreateIndex(
                name: "IX_WishlistItems_AppUserId_ProductId",
                table: "WishlistItems",
                columns: new[] { "AppUserId", "ProductId" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WishlistItems_AppUserId_ProductId",
                table: "WishlistItems");

            migrationBuilder.CreateIndex(
                name: "IX_WishlistItems_AppUserId_ProductId",
                table: "WishlistItems",
                columns: new[] { "AppUserId", "ProductId" },
                unique: true);
        }
    }
}
