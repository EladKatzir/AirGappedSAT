using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SatPrep.Data.Migrations
{
    /// <inheritdoc />
    public partial class HtmlQuestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsHtml",
                table: "Questions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsHtml",
                table: "Questions");
        }
    }
}
