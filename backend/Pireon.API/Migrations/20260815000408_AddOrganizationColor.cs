using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pireon.API.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Organizations",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Color",
                table: "Organizations");
        }
    }
}
