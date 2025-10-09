using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArticleService.Migrations.Global
{
    /// <inheritdoc />
    public partial class RemoveContinentFromGlobal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceArticleId",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "SourceContinent",
                table: "Articles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SourceArticleId",
                table: "Articles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceContinent",
                table: "Articles",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
