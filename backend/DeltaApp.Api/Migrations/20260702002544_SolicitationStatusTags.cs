using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeltaApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class SolicitationStatusTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Solicitations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "Solicitations",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Solicitations");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Solicitations");
        }
    }
}
