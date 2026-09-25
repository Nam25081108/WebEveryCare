using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EveryCare.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHospitalityBookingDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccommodationType",
                table: "bookings",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactName",
                table: "bookings",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPhone",
                table: "bookings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FacilityName",
                table: "bookings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccommodationType",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "ContactName",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "ContactPhone",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "FacilityName",
                table: "bookings");
        }
    }
}
