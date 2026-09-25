using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EveryCare.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurringServiceContracts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OccurrenceNumber",
                table: "bookings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RecurringContractId",
                table: "bookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "recurring_service_contracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServicePackageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RecurrenceRule = table.Column<string>(type: "text", nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ContractMonths = table.Column<int>(type: "integer", nullable: false),
                    TotalOccurrences = table.Column<int>(type: "integer", nullable: false),
                    CompletedOccurrences = table.Column<int>(type: "integer", nullable: false),
                    EstimatedTotal = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    DepositAmount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recurring_service_contracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_recurring_service_contracts_service_groups_ServiceGroupId",
                        column: x => x.ServiceGroupId,
                        principalTable: "service_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_recurring_service_contracts_service_packages_ServicePackage~",
                        column: x => x.ServicePackageId,
                        principalTable: "service_packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_recurring_service_contracts_users_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bookings_RecurringContractId_OccurrenceNumber",
                table: "bookings",
                columns: new[] { "RecurringContractId", "OccurrenceNumber" },
                unique: true,
                filter: "\"RecurringContractId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_recurring_service_contracts_Code",
                table: "recurring_service_contracts",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_recurring_service_contracts_CustomerId",
                table: "recurring_service_contracts",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_recurring_service_contracts_ServiceGroupId",
                table: "recurring_service_contracts",
                column: "ServiceGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_recurring_service_contracts_ServicePackageId",
                table: "recurring_service_contracts",
                column: "ServicePackageId");

            migrationBuilder.AddForeignKey(
                name: "FK_bookings_recurring_service_contracts_RecurringContractId",
                table: "bookings",
                column: "RecurringContractId",
                principalTable: "recurring_service_contracts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_bookings_recurring_service_contracts_RecurringContractId",
                table: "bookings");

            migrationBuilder.DropTable(
                name: "recurring_service_contracts");

            migrationBuilder.DropIndex(
                name: "IX_bookings_RecurringContractId_OccurrenceNumber",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "OccurrenceNumber",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "RecurringContractId",
                table: "bookings");
        }
    }
}
