using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GamingPlatform.Repository.Migrations
{
    /// <inheritdoc />
    public partial class m20 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GamerAchievements");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GamerAchievements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AchievementId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DateUnlocked = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GamerId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GamerAchievements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GamerAchievements_Achievements_AchievementId",
                        column: x => x.AchievementId,
                        principalTable: "Achievements",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_GamerAchievements_AchievementId",
                table: "GamerAchievements",
                column: "AchievementId");
        }
    }
}
