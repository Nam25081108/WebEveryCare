using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EveryCare.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingLifecycleAndDepositPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DepositAmount",
                table: "payments",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DepositPaidAt",
                table: "payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PlatformFee",
                table: "payments",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RefundedAmount",
                table: "payments",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReleasedAt",
                table: "payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RemainingAmount",
                table: "payments",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RemainingPaidAt",
                table: "payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaskerNetAmount",
                table: "payments",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "AvatarUrl",
                table: "partner_profiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WalletBalance",
                table: "partner_profiles",
                type: "numeric(14,2)",
                precision: 14,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ArrivedAt",
                table: "bookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CompletionReportedAt",
                table: "bookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CustomerConfirmedAt",
                table: "bookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IssueNote",
                table: "bookings",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "IssueReportedAt",
                table: "bookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TaskerConfirmedAt",
                table: "bookings",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DepositAmount",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "DepositPaidAt",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "PlatformFee",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "RefundedAmount",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "ReleasedAt",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "RemainingAmount",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "RemainingPaidAt",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "TaskerNetAmount",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "AvatarUrl",
                table: "partner_profiles");

            migrationBuilder.DropColumn(
                name: "WalletBalance",
                table: "partner_profiles");

            migrationBuilder.DropColumn(
                name: "ArrivedAt",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "CompletionReportedAt",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "CustomerConfirmedAt",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "IssueNote",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "IssueReportedAt",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "TaskerConfirmedAt",
                table: "bookings");
        }
    }
}
