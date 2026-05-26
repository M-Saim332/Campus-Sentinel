using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusSentinel.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentSessionAndRegNo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── All other tables (Users, Visitors, AccessLogs, BlacklistLogs, Students) ──
            // already exist in the database (created via Schema.sql).
            // We only need to add the NEW columns to Students and rename/adjust QrCodeId.

            // 1. Rename BarcodeId → QrCodeId if the old column still exists
            migrationBuilder.Sql(@"
                IF COL_LENGTH('Students', 'BarcodeId') IS NOT NULL
                    AND COL_LENGTH('Students', 'QrCodeId') IS NULL
                BEGIN
                    EXEC sp_rename 'Students.BarcodeId', 'QrCodeId', 'COLUMN';
                END
            ");

            // 2. Add Session column (nullable first so existing rows don't break, then default it)
            migrationBuilder.Sql(@"
                IF COL_LENGTH('Students', 'Session') IS NULL
                BEGIN
                    ALTER TABLE Students ADD [Session] INT NOT NULL DEFAULT 2025;
                END
            ");

            // 3. Add RegistrationNo column
            migrationBuilder.Sql(@"
                IF COL_LENGTH('Students', 'RegistrationNo') IS NULL
                BEGIN
                    ALTER TABLE Students ADD RegistrationNo NVARCHAR(50) NOT NULL DEFAULT '';
                END
            ");

            // 4. Resize Department to NVARCHAR(20) if it's wider (safe to run even if already correct)
            migrationBuilder.Sql(@"
                IF COL_LENGTH('Students', 'Department') IS NOT NULL
                BEGIN
                    ALTER TABLE Students ALTER COLUMN Department NVARCHAR(20) NULL;
                END
            ");

            // 5. Ensure __EFMigrationsHistory table exists so EF can track this migration
            migrationBuilder.Sql(@"
                IF OBJECT_ID('__EFMigrationsHistory') IS NULL
                BEGIN
                    CREATE TABLE [__EFMigrationsHistory] (
                        [MigrationId] nvarchar(150) NOT NULL,
                        [ProductVersion] nvarchar(32) NOT NULL,
                        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
                    );
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse: drop the new columns (leave BarcodeId rename in place to avoid data loss)
            migrationBuilder.Sql(@"
                IF COL_LENGTH('Students', 'Session') IS NOT NULL
                    ALTER TABLE Students DROP COLUMN [Session];
                IF COL_LENGTH('Students', 'RegistrationNo') IS NOT NULL
                    ALTER TABLE Students DROP COLUMN RegistrationNo;
            ");
        }
    }
}
