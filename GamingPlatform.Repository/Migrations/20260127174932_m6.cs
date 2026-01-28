using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GamingPlatform.Repository.Migrations
{
    /// <inheritdoc />
    public partial class m6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Friends_Gamers_FriendId",
                table: "Friends");

            migrationBuilder.DropForeignKey(
                name: "FK_Friends_Gamers_UserId",
                table: "Friends");

            migrationBuilder.DropForeignKey(
                name: "FK_GamerAchievements_Gamers_GamerId",
                table: "GamerAchievements");

            migrationBuilder.DropForeignKey(
                name: "FK_Libraries_Gamers_GamerId",
                table: "Libraries");

            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Gamers_GamerId",
                table: "Reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_Wishlists_Gamers_GamerId",
                table: "Wishlists");

            migrationBuilder.DropTable(
                name: "Gamers");

            migrationBuilder.DropIndex(
                name: "IX_Wishlists_GamerId",
                table: "Wishlists");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_GamerId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Libraries_GamerId",
                table: "Libraries");

            migrationBuilder.DropIndex(
                name: "IX_GamerAchievements_GamerId",
                table: "GamerAchievements");

            migrationBuilder.AlterColumn<string>(
                name: "GamerId",
                table: "Wishlists",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "GamerId",
                table: "Reviews",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "GamerId",
                table: "Libraries",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "GamerId",
                table: "GamerAchievements",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Friends",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<string>(
                name: "FriendId",
                table: "Friends",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddForeignKey(
                name: "FK_Friends_AspNetUsers_FriendId",
                table: "Friends",
                column: "FriendId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Friends_AspNetUsers_UserId",
                table: "Friends",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Friends_AspNetUsers_FriendId",
                table: "Friends");

            migrationBuilder.DropForeignKey(
                name: "FK_Friends_AspNetUsers_UserId",
                table: "Friends");

            migrationBuilder.AlterColumn<Guid>(
                name: "GamerId",
                table: "Wishlists",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "GamerId",
                table: "Reviews",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "GamerId",
                table: "Libraries",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "GamerId",
                table: "GamerAchievements",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Friends",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<Guid>(
                name: "FriendId",
                table: "Friends",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateTable(
                name: "Gamers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateJoined = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProfilePicture = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gamers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Wishlists_GamerId",
                table: "Wishlists",
                column: "GamerId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_GamerId",
                table: "Reviews",
                column: "GamerId");

            migrationBuilder.CreateIndex(
                name: "IX_Libraries_GamerId",
                table: "Libraries",
                column: "GamerId");

            migrationBuilder.CreateIndex(
                name: "IX_GamerAchievements_GamerId",
                table: "GamerAchievements",
                column: "GamerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Friends_Gamers_FriendId",
                table: "Friends",
                column: "FriendId",
                principalTable: "Gamers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Friends_Gamers_UserId",
                table: "Friends",
                column: "UserId",
                principalTable: "Gamers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GamerAchievements_Gamers_GamerId",
                table: "GamerAchievements",
                column: "GamerId",
                principalTable: "Gamers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Libraries_Gamers_GamerId",
                table: "Libraries",
                column: "GamerId",
                principalTable: "Gamers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Gamers_GamerId",
                table: "Reviews",
                column: "GamerId",
                principalTable: "Gamers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Wishlists_Gamers_GamerId",
                table: "Wishlists",
                column: "GamerId",
                principalTable: "Gamers",
                principalColumn: "Id");
        }
    }
}
