using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArticleService.Migrations.Global
{
    /// <inheritdoc />
    public partial class AddSourceContinentToGlobal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceContinent",
                table: "Articles",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceContinent",
                table: "Articles");
        }
    }
}
