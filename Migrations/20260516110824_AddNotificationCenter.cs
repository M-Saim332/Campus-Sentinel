using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CampusSentinel.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationCenter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "GuardShifts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.CreateTable(
                name: "NotificationLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    RecipientUserId = table.Column<int>(type: "int", nullable: false),
                    RecipientContact = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    RenderedBody = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RelatedEntityId = table.Column<int>(type: "int", nullable: true),
                    RelatedEntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationLogs_Users_RecipientUserId",
                        column: x => x.RecipientUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotificationTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BodyTemplate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserNotificationPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    InAppEnabled = table.Column<bool>(type: "bit", nullable: false),
                    EmailEnabled = table.Column<bool>(type: "bit", nullable: false),
                    SmsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotificationPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserNotificationPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "NotificationTemplates",
                columns: new[] { "Id", "BodyTemplate", "Channel", "EventType", "IsActive", "Subject" },
                values: new object[,]
                {
                    { 1, "SECURITY ALERT: Blacklisted QR Code {{PersonName}} scanned at {{Location}} at {{Time}}.", 0, 0, true, null },
                    { 2, "A blacklisted person ({{PersonName}}) attempted entry at {{Location}} at {{Time}}.", 1, 0, true, "Campus Sentinel - Blacklist Alert" },
                    { 3, "CAPACITY WARNING: Campus occupancy has reached {{Occupancy}}% ({{CurrentCount}}/{{MaxCapacity}}).", 0, 1, true, null },
                    { 4, "AFTER-HOURS ACCESS: {{PersonName}} scanned at {{Location}} at {{Time}}.", 0, 2, true, null },
                    { 5, "NEW INCIDENT: A {{SeverityLevel}} incident '{{Title}}' was reported at {{Location}}.", 0, 3, true, null },
                    { 6, "A new {{SeverityLevel}} severity incident has been logged: {{Title}}\nLocation: {{Location}}\nTime: {{Time}}", 1, 3, true, "Campus Sentinel - New Incident Reported" },
                    { 7, "MISSED SHIFT: Guard {{PersonName}} missed their scheduled shift at {{Location}} starting at {{Time}}.", 0, 4, true, null },
                    { 8, "UNAUTHORIZED SCAN: An invalid or unauthorized QR code was scanned at {{Location}} at {{Time}}.", 0, 5, true, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLogs_RecipientUserId",
                table: "NotificationLogs",
                column: "RecipientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotificationPreferences_UserId",
                table: "UserNotificationPreferences",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationLogs");

            migrationBuilder.DropTable(
                name: "NotificationTemplates");

            migrationBuilder.DropTable(
                name: "UserNotificationPreferences");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "GuardShifts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);
        }
    }
}
