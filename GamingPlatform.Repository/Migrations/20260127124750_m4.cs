using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GamingPlatform.Repository.Migrations
{
    /// <inheritdoc />
    public partial class m4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Title",
                table: "Achievements",
                newName: "Name");

            migrationBuilder.AddColumn<string>(
                name: "SystemRequirements",
                table: "Games",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SystemRequirements",
                table: "Games");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Achievements",
                newName: "Title");
        }
    }
}
