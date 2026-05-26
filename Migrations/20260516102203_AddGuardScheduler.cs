using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CampusSentinel.Migrations
{
    /// <inheritdoc />
    public partial class AddGuardScheduler : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CampusZones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampusZones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuardShifts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GuardUserId = table.Column<int>(type: "int", nullable: false),
                    ZoneId = table.Column<int>(type: "int", nullable: false),
                    ShiftDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedByAdminId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuardShifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuardShifts_CampusZones_ZoneId",
                        column: x => x.ZoneId,
                        principalTable: "CampusZones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GuardShifts_Users_CreatedByAdminId",
                        column: x => x.CreatedByAdminId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GuardShifts_Users_GuardUserId",
                        column: x => x.GuardUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShiftSwapRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestingGuardId = table.Column<int>(type: "int", nullable: false),
                    TargetGuardId = table.Column<int>(type: "int", nullable: false),
                    ShiftId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedByAdminId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftSwapRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShiftSwapRequests_GuardShifts_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "GuardShifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShiftSwapRequests_Users_RequestingGuardId",
                        column: x => x.RequestingGuardId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShiftSwapRequests_Users_ResolvedByAdminId",
                        column: x => x.ResolvedByAdminId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShiftSwapRequests_Users_TargetGuardId",
                        column: x => x.TargetGuardId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "CampusZones",
                columns: new[] { "Id", "Description", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, "Primary entrance for all visitors and vehicles", true, "Main Gate" },
                    { 2, "Central library and study area", true, "Library Block" },
                    { 3, "Staff and premium student parking", true, "Parking Lot A" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuardShifts_CreatedByAdminId",
                table: "GuardShifts",
                column: "CreatedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_GuardShifts_GuardUserId",
                table: "GuardShifts",
                column: "GuardUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GuardShifts_ZoneId",
                table: "GuardShifts",
                column: "ZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftSwapRequests_RequestingGuardId",
                table: "ShiftSwapRequests",
                column: "RequestingGuardId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftSwapRequests_ResolvedByAdminId",
                table: "ShiftSwapRequests",
                column: "ResolvedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftSwapRequests_ShiftId",
                table: "ShiftSwapRequests",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftSwapRequests_TargetGuardId",
                table: "ShiftSwapRequests",
                column: "TargetGuardId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShiftSwapRequests");

            migrationBuilder.DropTable(
                name: "GuardShifts");

            migrationBuilder.DropTable(
                name: "CampusZones");
        }
    }
}
