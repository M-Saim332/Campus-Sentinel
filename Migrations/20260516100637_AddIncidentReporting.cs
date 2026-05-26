using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusSentinel.Migrations
{
    /// <inheritdoc />
    public partial class AddIncidentReporting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Incidents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ReportedById = table.Column<int>(type: "int", nullable: false),
                    ReportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LinkedPersonId = table.Column<int>(type: "int", nullable: true),
                    LinkedPersonType = table.Column<int>(type: "int", nullable: true),
                    LinkedAccessLogId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incidents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Incidents_AccessLogs_LinkedAccessLogId",
                        column: x => x.LinkedAccessLogId,
                        principalTable: "AccessLogs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Incidents_Users_ReportedById",
                        column: x => x.ReportedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IncidentNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IncidentId = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AddedById = table.Column<int>(type: "int", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncidentNotes_Incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "Incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IncidentNotes_Users_AddedById",
                        column: x => x.AddedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IncidentNotes_AddedById",
                table: "IncidentNotes",
                column: "AddedById");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentNotes_IncidentId",
                table: "IncidentNotes",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_LinkedAccessLogId",
                table: "Incidents",
                column: "LinkedAccessLogId");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_ReportedById",
                table: "Incidents",
                column: "ReportedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IncidentNotes");

            migrationBuilder.DropTable(
                name: "Incidents");
        }
    }
}
