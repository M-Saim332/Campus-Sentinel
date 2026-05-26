using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusSentinel.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessDirectionAndAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Direction",
                table: "AccessLogs",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Direction",
                table: "AccessLogs");
        }
    }
}
