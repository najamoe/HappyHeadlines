using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DraftService.Migrations
{
    /// <inheritdoc />
    public partial class addContinentToDraft : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Continent",
                table: "Drafts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Continent",
                table: "Drafts");
        }
    }
}
