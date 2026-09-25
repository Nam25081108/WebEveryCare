using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EveryCare.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameRoomCleaningService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE service_groups SET \"Name\" = 'Dọn dẹp nhà cửa' WHERE \"Slug\" = 've-sinh-phong-le';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE service_groups SET \"Name\" = 'Vệ sinh phòng lẻ' WHERE \"Slug\" = 've-sinh-phong-le';");
        }
    }
}
