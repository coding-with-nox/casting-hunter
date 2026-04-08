using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CastingRadar.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSourceUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "Sources",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Url",
                table: "Sources");
        }
    }
}
