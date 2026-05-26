-- =============================================================================
-- Campus Sentinel — Challan Generation Feature
-- Migration: AddChallansTable
-- Date:       2026-05-26
-- Description:
--   Creates the [Challans] table which records every digital fine / penalty
--   issued by an authorised campus official against a Student, Staff member
--   or Visitor identified via their unique QR code.
--
-- Run this script once against the existing CampusSentinel LocalDB.
-- Command:  sqlcmd -S "(localdb)\mssqllocaldb" -d CampusSentinel -i AddChallansTable.sql
-- =============================================================================

USE [CampusSentinel];
GO

-- ── Guard: skip if the table already exists ──────────────────────────────────
IF OBJECT_ID(N'dbo.Challans', N'U') IS NOT NULL
BEGIN
    PRINT 'Table [Challans] already exists — skipping creation.';
    RETURN;
END
GO

-- ── Create the Challans table ─────────────────────────────────────────────────
CREATE TABLE [dbo].[Challans]
(
    -- Primary key — auto-incrementing identity so EF can map it cleanly
    [Id]              INT             NOT NULL IDENTITY(1,1),

    -- The scanned QR code string that ties this challan to a person.
    -- Matches Student.QrCodeId  | Staff.StaffId | Visitor.TemporaryQrCodeId.
    -- Stored as a string reference (same pattern as AccessLogs.TargetId)
    -- because subjects live in three separate, non-unified tables.
    [QrCodeId]        NVARCHAR(50)    NOT NULL,

    -- Denormalised display fields resolved at issuance time so the challan
    -- record remains readable even if the source person record changes later.
    [SubjectName]     NVARCHAR(100)   NOT NULL,
    [SubjectType]     NVARCHAR(20)    NOT NULL,  -- 'Student' | 'Staff' | 'Visitor'

    -- Violation details
    [ViolationType]   NVARCHAR(100)   NOT NULL,
    [Description]     NVARCHAR(MAX)   NULL,

    -- Financial fields — Amount is stored as DECIMAL(10,2) for precision
    [Amount]          DECIMAL(10, 2)  NOT NULL
        CONSTRAINT [CK_Challans_Amount_Positive] CHECK ([Amount] > 0),

    -- Audit trail — who issued it and when
    [IssuedByUserId]  INT             NOT NULL,
    [IssueDate]       DATETIME2       NOT NULL  DEFAULT SYSUTCDATETIME(),

    -- Lifecycle status (maps to ChallanStatus enum):
    --   0 = Pending | 1 = Paid | 2 = Disputed | 3 = Cancelled
    [Status]          INT             NOT NULL  DEFAULT 0,

    -- Optional notes added during status updates (e.g. payment reference)
    [Notes]           NVARCHAR(500)   NULL,

    -- ── Constraints ──────────────────────────────────────────────────────────
    CONSTRAINT [PK_Challans] PRIMARY KEY CLUSTERED ([Id] ASC),

    -- Foreign key to the system Users table (the official who issued the fine).
    -- ON DELETE RESTRICT: removing a user account cannot silently orphan records.
    CONSTRAINT [FK_Challans_Users_IssuedByUserId]
        FOREIGN KEY ([IssuedByUserId])
        REFERENCES [dbo].[Users] ([Id])
        ON DELETE NO ACTION
        ON UPDATE NO ACTION
);
GO

-- ── Performance indexes ───────────────────────────────────────────────────────

-- High-traffic use-case 1: "Show all challans for a scanned QR code"
CREATE NONCLUSTERED INDEX [IX_Challans_QrCodeId]
    ON [dbo].[Challans] ([QrCodeId] ASC);
GO

-- High-traffic use-case 2: "Show all Pending challans" (dashboard / reports)
CREATE NONCLUSTERED INDEX [IX_Challans_Status]
    ON [dbo].[Challans] ([Status] ASC);
GO

-- Combined index for the common query pattern: filter by QR AND status
CREATE NONCLUSTERED INDEX [IX_Challans_QrCodeId_Status]
    ON [dbo].[Challans] ([QrCodeId] ASC, [Status] ASC);
GO

-- Index to support looking up challans issued by a specific official
CREATE NONCLUSTERED INDEX [IX_Challans_IssuedByUserId]
    ON [dbo].[Challans] ([IssuedByUserId] ASC);
GO

PRINT 'Table [Challans] and all indexes created successfully.';
GO
