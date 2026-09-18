using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EveryCare.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpandEveryCareServiceCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CategorySlug",
                table: "service_groups",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "cleaning");

            migrationBuilder.AddColumn<bool>(
                name: "IsComingSoon",
                table: "service_groups",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CategorySlug",
                table: "service_groups");

            migrationBuilder.DropColumn(
                name: "IsComingSoon",
                table: "service_groups");
        }
    }
}
