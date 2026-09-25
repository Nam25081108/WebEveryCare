using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace EveryCare.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CompletePartnerWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovalEmailError",
                table: "partner_profiles",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ApprovalEmailSentAt",
                table: "partner_profiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "partner_profiles",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReviewedAt",
                table: "partner_profiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceAddress",
                table: "partner_profiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Point>(
                name: "ServiceLocation",
                table: "partner_profiles",
                type: "geography (point)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ServiceRadiusKilometers",
                table: "partner_profiles",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 10m);

            migrationBuilder.Sql("""
                UPDATE partner_profiles
                SET "ServiceLocation" = "CurrentLocation",
                    "ServiceAddress" = CASE WHEN "ServiceAddress" = '' THEN 'Địa chỉ đã đăng ký' ELSE "ServiceAddress" END
                WHERE "ServiceLocation" IS NULL;
                """);

            migrationBuilder.CreateTable(
                name: "partner_availability_overrides",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PartnerProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    IsUnavailable = table.Column<bool>(type: "boolean", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    Note = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_partner_availability_overrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_partner_availability_overrides_partner_profiles_PartnerProf~",
                        column: x => x.PartnerProfileId,
                        principalTable: "partner_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "partner_availability_rules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PartnerProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_partner_availability_rules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_partner_availability_rules_partner_profiles_PartnerProfileId",
                        column: x => x.PartnerProfileId,
                        principalTable: "partner_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "partner_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_partner_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_partner_sessions_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO partner_availability_rules
                    ("Id", "PartnerProfileId", "DayOfWeek", "StartTime", "EndTime", "IsActive", "CreatedAt", "IsDeleted")
                SELECT gen_random_uuid(), p."Id", d.day, TIME '08:00', TIME '17:00', TRUE, CURRENT_TIMESTAMP, FALSE
                FROM partner_profiles p
                CROSS JOIN (VALUES (1), (2), (3), (4), (5), (6)) AS d(day)
                WHERE p."VerificationStatus" = 'Approved';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_partner_profiles_ServiceLocation",
                table: "partner_profiles",
                column: "ServiceLocation")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_partner_availability_overrides_PartnerProfileId_Date",
                table: "partner_availability_overrides",
                columns: new[] { "PartnerProfileId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_partner_availability_rules_PartnerProfileId_DayOfWeek",
                table: "partner_availability_rules",
                columns: new[] { "PartnerProfileId", "DayOfWeek" });

            migrationBuilder.CreateIndex(
                name: "IX_partner_sessions_TokenHash",
                table: "partner_sessions",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_partner_sessions_UserId_ExpiresAt",
                table: "partner_sessions",
                columns: new[] { "UserId", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "partner_availability_overrides");

            migrationBuilder.DropTable(
                name: "partner_availability_rules");

            migrationBuilder.DropTable(
                name: "partner_sessions");

            migrationBuilder.DropIndex(
                name: "IX_partner_profiles_ServiceLocation",
                table: "partner_profiles");

            migrationBuilder.DropColumn(
                name: "ApprovalEmailError",
                table: "partner_profiles");

            migrationBuilder.DropColumn(
                name: "ApprovalEmailSentAt",
                table: "partner_profiles");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "partner_profiles");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "partner_profiles");

            migrationBuilder.DropColumn(
                name: "ServiceAddress",
                table: "partner_profiles");

            migrationBuilder.DropColumn(
                name: "ServiceLocation",
                table: "partner_profiles");

            migrationBuilder.DropColumn(
                name: "ServiceRadiusKilometers",
                table: "partner_profiles");
        }
    }
}
