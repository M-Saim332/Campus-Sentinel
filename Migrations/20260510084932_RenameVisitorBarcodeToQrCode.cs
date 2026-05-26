using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusSentinel.Migrations
{
    /// <inheritdoc />
    public partial class RenameVisitorBarcodeToQrCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF COL_LENGTH('Visitors', 'TemporaryBarcodeId') IS NOT NULL
                    AND COL_LENGTH('Visitors', 'TemporaryQrCodeId') IS NULL
                BEGIN
                    EXEC sp_rename 'Visitors.TemporaryBarcodeId', 'TemporaryQrCodeId', 'COLUMN';
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF COL_LENGTH('Visitors', 'TemporaryQrCodeId') IS NOT NULL
                    AND COL_LENGTH('Visitors', 'TemporaryBarcodeId') IS NULL
                BEGIN
                    EXEC sp_rename 'Visitors.TemporaryQrCodeId', 'TemporaryBarcodeId', 'COLUMN';
                END
            ");
        }
    }
}
