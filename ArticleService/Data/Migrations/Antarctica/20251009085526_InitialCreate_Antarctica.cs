using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArticleService.Data.Migrations.Antarctica
{
    /// <inheritdoc />
    public partial class InitialCreate_Antarctica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Continent",
                table: "Articles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Continent",
                table: "Articles");
        }
    }
}
