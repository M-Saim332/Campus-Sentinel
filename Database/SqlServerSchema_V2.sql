-- =============================================================================
-- Campus Sentinel — Advanced SQL Server Database Programmability Objects
-- File: SqlServerSchema_V2.sql
-- Description:
--   Creates indexes, user-defined functions, triggers, and stored procedures
--   to enhance database integrity, performance, and robustness under MS SQL Server.
-- =============================================================================

-- =============================================================================
-- 1. INDEXES FOR PERFORMANCE OPTIMIZATION
-- =============================================================================

-- High-traffic lookup: Guard Shifts Weekly Roster and Availability checking
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GuardShifts_GuardUserId_ShiftDate' AND object_id = OBJECT_ID('dbo.GuardShifts'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_GuardShifts_GuardUserId_ShiftDate
    ON dbo.GuardShifts (GuardUserId, ShiftDate)
    INCLUDE (ZoneId, StartTime, EndTime, Status);
END
GO

-- High-traffic lookup: AccessLogs scan validations and history queries
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AccessLogs_TargetId_Timestamp' AND object_id = OBJECT_ID('dbo.AccessLogs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_AccessLogs_TargetId_Timestamp
    ON dbo.AccessLogs (TargetId, Timestamp)
    INCLUDE (Status, Direction);
END
GO

-- High-traffic lookup: Incident analytics and dashboard statistics
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Incidents_Status_Severity' AND object_id = OBJECT_ID('dbo.Incidents'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Incidents_Status_Severity
    ON dbo.Incidents (Status, Severity)
    INCLUDE (Title, Location, ReportedAt);
END
GO


-- =============================================================================
-- 2. USER-DEFINED FUNCTIONS (UDFs)
-- =============================================================================

-- Function: Count pending (unpaid) challans for a given QR Code / Person ID
CREATE OR ALTER FUNCTION dbo.fn_GetPendingChallanCount (@QrCodeId NVARCHAR(50))
RETURNS INT
AS
BEGIN
    DECLARE @Count INT;
    -- Status = 0 corresponds to ChallanStatus.Pending
    SELECT @Count = COUNT(*) 
    FROM dbo.Challans 
    WHERE QrCodeId = @QrCodeId AND Status = 0;
    
    RETURN ISNULL(@Count, 0);
END;
GO

-- Function: Check if a guard is available for a shift without overlaps
CREATE OR ALTER FUNCTION dbo.fn_CheckGuardAvailability (
    @GuardUserId INT,
    @ShiftDate DATE,
    @StartTime TIME,
    @EndTime TIME,
    @ExcludeShiftId INT = 0
)
RETURNS BIT
AS
BEGIN
    DECLARE @IsAvailable BIT = 1;
    DECLARE @OverlapCount INT = 0;

    -- Overlap condition: ShiftDate is same, status is active/scheduled (0=Scheduled, 1=Active),
    -- and time ranges overlap (StartTime < Existing.EndTime AND EndTime > Existing.StartTime)
    SELECT @OverlapCount = COUNT(*)
    FROM dbo.GuardShifts
    WHERE GuardUserId = @GuardUserId
      AND ShiftDate = @ShiftDate
      AND Id <> @ExcludeShiftId
      AND Status IN (0, 1)
      AND @StartTime < EndTime
      AND @EndTime > StartTime;

    IF @OverlapCount > 0
        SET @IsAvailable = 0;

    RETURN @IsAvailable;
END;
GO

-- Function: Get total incident counts logged in a specific zone/location
CREATE OR ALTER FUNCTION dbo.fn_GetIncidentCountForZone (@ZoneName NVARCHAR(50))
RETURNS INT
AS
BEGIN
    DECLARE @Count INT;
    SELECT @Count = COUNT(*)
    FROM dbo.Incidents
    WHERE Location = @ZoneName;
    
    RETURN ISNULL(@Count, 0);
END;
GO


-- =============================================================================
-- 3. TRIGGERS (INTEGRITY & AUTO-BLACKLISTING)
-- =============================================================================

-- Trigger: Auto-blacklist a person if they accumulate 3 or more pending challans,
-- and auto-clear blacklist status once unpaid challans drop below 3.
CREATE OR ALTER TRIGGER trg_Challans_AutoBlacklist
ON dbo.Challans
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @QrCodeId NVARCHAR(50);
    DECLARE @SubjectType NVARCHAR(20);
    DECLARE @PendingCount INT;

    -- Cursor to process multiple rows impacted in a single batch insert/update
    DECLARE db_cursor CURSOR LOCAL FAST_FORWARD FOR 
    SELECT DISTINCT QrCodeId, SubjectType 
    FROM inserted;

    OPEN db_cursor;
    FETCH NEXT FROM db_cursor INTO @QrCodeId, @SubjectType;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- Fetch pending count
        SET @PendingCount = dbo.fn_GetPendingChallanCount(@QrCodeId);

        IF @PendingCount >= 3
        BEGIN
            -- Set IsBlacklisted = 1 in respective table
            IF @SubjectType = 'Student'
                UPDATE dbo.Students SET IsBlacklisted = 1 WHERE QrCodeId = @QrCodeId;
            ELSE IF @SubjectType = 'Visitor'
                UPDATE dbo.Visitors SET IsBlacklisted = 1 WHERE TemporaryQrCodeId = @QrCodeId;
            ELSE IF @SubjectType = 'Staff'
                UPDATE dbo.Staff SET IsBlacklisted = 1 WHERE StaffId = @QrCodeId;

            -- Log to BlacklistLogs if not already registered for this reason
            IF NOT EXISTS (SELECT 1 FROM dbo.BlacklistLogs WHERE TargetId = @QrCodeId AND Reason LIKE 'Auto-blacklisted%')
            BEGIN
                INSERT INTO dbo.BlacklistLogs (TargetId, TargetType, Reason, BlacklistedAt, BlacklistedBy)
                VALUES (@QrCodeId, @SubjectType, 'Auto-blacklisted: accumulated ' + CAST(@PendingCount AS VARCHAR(5)) + ' pending challans.', GETDATE(), NULL);
            END
        END
        ELSE
        BEGIN
            -- If pending count is now less than 3, auto-restore access only if blacklisted via system trigger
            IF EXISTS (SELECT 1 FROM dbo.BlacklistLogs WHERE TargetId = @QrCodeId AND Reason LIKE 'Auto-blacklisted%')
            BEGIN
                IF @SubjectType = 'Student'
                    UPDATE dbo.Students SET IsBlacklisted = 0 WHERE QrCodeId = @QrCodeId;
                ELSE IF @SubjectType = 'Visitor'
                    UPDATE dbo.Visitors SET IsBlacklisted = 0 WHERE TemporaryQrCodeId = @QrCodeId;
                ELSE IF @SubjectType = 'Staff'
                    UPDATE dbo.Staff SET IsBlacklisted = 0 WHERE StaffId = @QrCodeId;

                -- Remove the auto-blacklist log entry
                DELETE FROM dbo.BlacklistLogs 
                WHERE TargetId = @QrCodeId AND Reason LIKE 'Auto-blacklisted%';
            END
        END

        FETCH NEXT FROM db_cursor INTO @QrCodeId, @SubjectType;
    END;

    CLOSE db_cursor;
    DEALLOCATE db_cursor;
END;
GO

-- Trigger: Guard Shift schedule overlap prevention at database level
CREATE OR ALTER TRIGGER trg_GuardShifts_PreventOverlap
ON dbo.GuardShifts
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Block inserts/updates that violate guard scheduling rules
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN dbo.GuardShifts gs ON i.GuardUserId = gs.GuardUserId 
            AND i.ShiftDate = gs.ShiftDate
            AND i.Id <> gs.Id
        WHERE i.Status IN (0, 1) -- 0 = Scheduled, 1 = Active
          AND gs.Status IN (0, 1)
          AND i.StartTime < gs.EndTime
          AND i.EndTime > gs.StartTime
    )
    BEGIN
        RAISERROR ('Guard Scheduling Conflict: Guard has another active or scheduled shift overlapping with this time interval.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO


-- =============================================================================
-- 4. STORED PROCEDURES (TRANSACTIONAL SAFETY)
-- =============================================================================

-- Procedure: Issue a Challan safely by resolving SubjectName & Type automatically
CREATE OR ALTER PROCEDURE dbo.sp_IssueChallan
    @QrCodeId NVARCHAR(50),
    @ViolationType NVARCHAR(100),
    @Description NVARCHAR(MAX),
    @Amount DECIMAL(10,2),
    @IssuedByUserId INT,
    @Notes NVARCHAR(500) = NULL,
    @NewChallanId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @SubjectName NVARCHAR(100) = NULL;
    DECLARE @SubjectType NVARCHAR(20) = NULL;

    -- 1. Try to find the person in Students
    SELECT @SubjectName = FullName, @SubjectType = 'Student'
    FROM dbo.Students
    WHERE QrCodeId = @QrCodeId;

    -- 2. Try Staff if not found
    IF @SubjectName IS NULL
    BEGIN
        SELECT @SubjectName = FullName, @SubjectType = 'Staff'
        FROM dbo.Staff
        WHERE StaffId = @QrCodeId;
    END

    -- 3. Try Visitors if not found
    IF @SubjectName IS NULL
    BEGIN
        SELECT @SubjectName = FullName, @SubjectType = 'Visitor'
        FROM dbo.Visitors
        WHERE TemporaryQrCodeId = @QrCodeId;
    END

    -- 4. Validation checks
    IF @SubjectName IS NULL
    BEGIN
        RAISERROR ('Validation Error: The provided QR Code / ID does not match any registered Student, Staff member, or Visitor.', 16, 1);
        RETURN;
    END

    IF @Amount <= 0
    BEGIN
        RAISERROR ('Validation Error: Challan amount must be greater than zero.', 16, 1);
        RETURN;
    END

    -- 5. Insert challan
    INSERT INTO dbo.Challans (
        QrCodeId,
        SubjectName,
        SubjectType,
        ViolationType,
        Description,
        Amount,
        IssuedByUserId,
        IssueDate,
        Status,
        Notes
    )
    VALUES (
        @QrCodeId,
        @SubjectName,
        @SubjectType,
        @ViolationType,
        @Description,
        @Amount,
        @IssuedByUserId,
        GETUTCDATE(),
        0, -- 0 = Pending
        @Notes
    );

    SET @NewChallanId = SCOPE_IDENTITY();
END;
GO

-- Procedure: Approve or reject a shift swap safely under a database transaction
CREATE OR ALTER PROCEDURE dbo.sp_ResolveShiftSwapRequest
    @SwapRequestId INT,
    @AdminUserId INT,
    @Approved BIT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRANSACTION;
    BEGIN TRY
        DECLARE @Status INT;
        DECLARE @ShiftId INT;
        DECLARE @RequestingGuardId INT;
        DECLARE @TargetGuardId INT;

        -- Check request details
        SELECT @Status = Status, @ShiftId = ShiftId, @RequestingGuardId = RequestingGuardId, @TargetGuardId = TargetGuardId
        FROM dbo.ShiftSwapRequests
        WHERE Id = @SwapRequestId;

        IF @Status IS NULL OR @Status <> 0 -- 0 = Pending
        BEGIN
            RAISERROR ('Error: Shift Swap Request is either invalid or already resolved.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- Set request resolution status (1 = Approved, 2 = Rejected)
        DECLARE @NewStatus INT = CASE WHEN @Approved = 1 THEN 1 ELSE 2 END;

        UPDATE dbo.ShiftSwapRequests
        SET Status = @NewStatus,
            ResolvedAt = GETDATE(),
            ResolvedByAdminId = @AdminUserId
        WHERE Id = @SwapRequestId;

        -- Apply swap if approved
        IF @Approved = 1
        BEGIN
            DECLARE @ShiftDate DATE;
            DECLARE @StartTime TIME;
            DECLARE @EndTime TIME;
            DECLARE @ZoneId INT;

            SELECT @ShiftDate = ShiftDate, @StartTime = StartTime, @EndTime = EndTime, @ZoneId = ZoneId
            FROM dbo.GuardShifts
            WHERE Id = @ShiftId;

            -- Check availability of target guard using availability function
            IF dbo.fn_CheckGuardAvailability(@TargetGuardId, @ShiftDate, @StartTime, @EndTime, 0) = 0
            BEGIN
                RAISERROR ('Conflict: The target guard is unavailable or has an overlapping shift assignment.', 16, 1);
                ROLLBACK TRANSACTION;
                RETURN;
            END

            -- Update original shift status (set guard to target guard, status to Swapped = 4)
            UPDATE dbo.GuardShifts
            SET GuardUserId = @TargetGuardId,
                Status = 4 -- 4 = Swapped
            WHERE Id = @ShiftId;

            -- Create a new scheduled shift for the target guard
            INSERT INTO dbo.GuardShifts (
                GuardUserId,
                ZoneId,
                ShiftDate,
                StartTime,
                EndTime,
                Status,
                CreatedByAdminId,
                CreatedAt,
                Notes
            )
            VALUES (
                @TargetGuardId,
                @ZoneId,
                @ShiftDate,
                @StartTime,
                @EndTime,
                0, -- 0 = Scheduled
                @AdminUserId,
                GETDATE(),
                'Swapped from guard ID: ' + CAST(@RequestingGuardId AS VARCHAR(10))
            );
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();

        RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END;
GO
