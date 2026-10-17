using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GamingPlatform.Repository.Migrations
{
    /// <inheritdoc />
    public partial class m17 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rows of users that no longer exist would block the new foreign keys.
            // Library/wishlist rows are deleted; reviews are kept with no author,
            // matching what SetNull does for future account deletions. Not undone by Down().
            migrationBuilder.Sql("DELETE FROM [Libraries] WHERE [GamerId] NOT IN (SELECT [Id] FROM [AspNetUsers]);");
            migrationBuilder.Sql("DELETE FROM [Wishlists] WHERE [GamerId] IS NOT NULL AND [GamerId] NOT IN (SELECT [Id] FROM [AspNetUsers]);");
            migrationBuilder.Sql("UPDATE [Reviews] SET [GamerId] = NULL WHERE [GamerId] IS NOT NULL AND [GamerId] NOT IN (SELECT [Id] FROM [AspNetUsers]);");

            migrationBuilder.AlterColumn<string>(
                name: "GamerId",
                table: "Wishlists",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "GamerId",
                table: "Reviews",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "GamerId",
                table: "Libraries",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Wishlists_GamerId_GameId",
                table: "Wishlists",
                columns: new[] { "GamerId", "GameId" },
                unique: true,
                filter: "[GamerId] IS NOT NULL AND [GameId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_GamerId_GameId",
                table: "Reviews",
                columns: new[] { "GamerId", "GameId" },
                unique: true,
                filter: "[GamerId] IS NOT NULL AND [GameId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Libraries_GamerId_GameId",
                table: "Libraries",
                columns: new[] { "GamerId", "GameId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Libraries_AspNetUsers_GamerId",
                table: "Libraries",
                column: "GamerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_AspNetUsers_GamerId",
                table: "Reviews",
                column: "GamerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Wishlists_AspNetUsers_GamerId",
                table: "Wishlists",
                column: "GamerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Libraries_AspNetUsers_GamerId",
                table: "Libraries");

            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_AspNetUsers_GamerId",
                table: "Reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_Wishlists_AspNetUsers_GamerId",
                table: "Wishlists");

            migrationBuilder.DropIndex(
                name: "IX_Wishlists_GamerId_GameId",
                table: "Wishlists");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_GamerId_GameId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Libraries_GamerId_GameId",
                table: "Libraries");

            migrationBuilder.AlterColumn<string>(
                name: "GamerId",
                table: "Wishlists",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "GamerId",
                table: "Reviews",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "GamerId",
                table: "Libraries",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
