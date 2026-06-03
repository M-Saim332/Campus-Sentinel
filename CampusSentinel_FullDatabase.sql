CREATE TABLE "CampusZones" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_CampusZones" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NOT NULL,
    "Description" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL
);


CREATE TABLE "NotificationTemplates" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_NotificationTemplates" PRIMARY KEY AUTOINCREMENT,
    "EventType" INTEGER NOT NULL,
    "Channel" INTEGER NOT NULL,
    "Subject" TEXT NULL,
    "BodyTemplate" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL
);


CREATE TABLE "Staff" (
    "Id" INTEGER NOT NULL CONSTRAINT  "PK_Staff" PRIMARY KEY AUTOINCREMENT,
    "StaffId" TEXT NOT NULL,
    "Category" INTEGER NOT NULL,
    "FullName" TEXT NOT NULL,
    "Designation" TEXT NOT NULL,
    "DepartmentOrUni" TEXT NOT NULL,
    "Gender" TEXT NOT NULL,
    "PhoneNumber" TEXT NULL,
    "IsBlacklisted" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL
);


CREATE TABLE "Students" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Students" PRIMARY KEY AUTOINCREMENT,
    "QrCodeId" TEXT NOT NULL,
    "Session" INTEGER NOT NULL,
    "FullName" TEXT NOT NULL,
    "Department" TEXT NOT NULL,
    "RegistrationNo" TEXT NOT NULL,
    "Gender" TEXT NOT NULL,
    "ResidencyType" TEXT NOT NULL,
    "IsBlacklisted" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL
);


CREATE TABLE "SystemSettings" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_SystemSettings" PRIMARY KEY AUTOINCREMENT,
    "Key" TEXT NOT NULL,
    "Value" TEXT NOT NULL,
    "Description" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL
);


CREATE TABLE "Users" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Users" PRIMARY KEY AUTOINCREMENT,
    "Username" TEXT NOT NULL,
    "PasswordHash" TEXT NOT NULL,
    "Role" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "IsActive" INTEGER NOT NULL
);


CREATE TABLE "Visitors" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Visitors" PRIMARY KEY AUTOINCREMENT,
    "TemporaryQrCodeId" TEXT NOT NULL,
    "FullName" TEXT NOT NULL,
    "Purpose" TEXT NULL,
    "Role" TEXT NOT NULL,
    "ExpirationTime" TEXT NOT NULL,
    "IsBlacklisted" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL
);


CREATE TABLE "AccessLogs" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_AccessLogs" PRIMARY KEY AUTOINCREMENT,
    "TargetId" TEXT NOT NULL,
    "TargetType" TEXT NOT NULL,
    "Timestamp" TEXT NOT NULL,
    "Status" TEXT NOT NULL,
    "Direction" TEXT NOT NULL,
    "Reason" TEXT NOT NULL,
    "GuardId" INTEGER NULL,
    "GateLocation" TEXT NOT NULL,
    CONSTRAINT "FK_AccessLogs_Users_GuardId" FOREIGN KEY ("GuardId") REFERENCES "Users" ("Id")
);


CREATE TABLE "BlacklistLogs" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_BlacklistLogs" PRIMARY KEY AUTOINCREMENT,
    "TargetId" TEXT NOT NULL,
    "TargetType" TEXT NOT NULL,
    "Reason" TEXT NOT NULL,
    "BlacklistedAt" TEXT NOT NULL,
    "BlacklistedBy" INTEGER NULL,
    CONSTRAINT "FK_BlacklistLogs_Users_BlacklistedBy" FOREIGN KEY ("BlacklistedBy") REFERENCES "Users" ("Id")
);


CREATE TABLE "Challans" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Challans" PRIMARY KEY AUTOINCREMENT,
    "QrCodeId" TEXT NOT NULL,
    "SubjectName" TEXT NOT NULL,
    "SubjectType" TEXT NOT NULL,
    "ViolationType" TEXT NOT NULL,
    "Description" TEXT NULL,
    "Amount" decimal(10,2) NOT NULL,
    "IssuedByUserId" INTEGER NOT NULL,
    "IssueDate" TEXT NOT NULL,
    "Status" INTEGER NOT NULL,
    "Notes" TEXT NULL,
    CONSTRAINT "FK_Challans_Users_IssuedByUserId" FOREIGN KEY ("IssuedByUserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "GuardShifts" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_GuardShifts" PRIMARY KEY AUTOINCREMENT,
    "GuardUserId" INTEGER NOT NULL,
    "ZoneId" INTEGER NOT NULL,
    "ShiftDate" TEXT NOT NULL,
    "StartTime" TEXT NOT NULL,
    "EndTime" TEXT NOT NULL,
    "Status" INTEGER NOT NULL,
    "Notes" TEXT NULL,
    "CreatedByAdminId" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    CONSTRAINT "FK_GuardShifts_CampusZones_ZoneId" FOREIGN KEY ("ZoneId") REFERENCES "CampusZones" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_GuardShifts_Users_CreatedByAdminId" FOREIGN KEY ("CreatedByAdminId") REFERENCES "Users" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_GuardShifts_Users_GuardUserId" FOREIGN KEY ("GuardUserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "NotificationLogs" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_NotificationLogs" PRIMARY KEY AUTOINCREMENT,
    "EventType" INTEGER NOT NULL,
    "Channel" INTEGER NOT NULL,
    "RecipientUserId" INTEGER NOT NULL,
    "RecipientContact" TEXT NULL,
    "RenderedBody" TEXT NOT NULL,
    "SentAt" TEXT NOT NULL,
    "Status" INTEGER NOT NULL,
    "ErrorMessage" TEXT NULL,
    "RelatedEntityId" INTEGER NULL,
    "RelatedEntityType" TEXT NULL,
    "IsRead" INTEGER NOT NULL,
    CONSTRAINT "FK_NotificationLogs_Users_RecipientUserId" FOREIGN KEY ("RecipientUserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "UserNotificationPreferences" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_UserNotificationPreferences" PRIMARY KEY AUTOINCREMENT,
    "UserId" INTEGER NOT NULL,
    "EventType" INTEGER NOT NULL,
    "InAppEnabled" INTEGER NOT NULL,
    "EmailEnabled" INTEGER NOT NULL,
    "SmsEnabled" INTEGER NOT NULL,
    "PhoneNumber" TEXT NULL,
    CONSTRAINT "FK_UserNotificationPreferences_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "Incidents" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Incidents" PRIMARY KEY AUTOINCREMENT,
    "Title" TEXT NOT NULL,
    "Description" TEXT NOT NULL,
    "Severity" INTEGER NOT NULL,
    "Status" INTEGER NOT NULL,
    "Location" TEXT NOT NULL,
    "ReportedById" INTEGER NOT NULL,
    "ReportedAt" TEXT NOT NULL,
    "ResolvedAt" TEXT NULL,
    "LinkedPersonId" INTEGER NULL,
    "LinkedPersonType" INTEGER NULL,
    "LinkedAccessLogId" INTEGER NULL,
    CONSTRAINT "FK_Incidents_AccessLogs_LinkedAccessLogId" FOREIGN KEY ("LinkedAccessLogId") REFERENCES "AccessLogs" ("Id"),
    CONSTRAINT "FK_Incidents_Users_ReportedById" FOREIGN KEY ("ReportedById") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "ShiftSwapRequests" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_ShiftSwapRequests" PRIMARY KEY AUTOINCREMENT,
    "RequestingGuardId" INTEGER NOT NULL,
    "TargetGuardId" INTEGER NOT NULL,
    "ShiftId" INTEGER NOT NULL,
    "Reason" TEXT NOT NULL,
    "Status" INTEGER NOT NULL,
    "RequestedAt" TEXT NOT NULL,
    "ResolvedAt" TEXT NULL,
    "ResolvedByAdminId" INTEGER NULL,
    CONSTRAINT "FK_ShiftSwapRequests_GuardShifts_ShiftId" FOREIGN KEY ("ShiftId") REFERENCES "GuardShifts" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ShiftSwapRequests_Users_RequestingGuardId" FOREIGN KEY ("RequestingGuardId") REFERENCES "Users" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_ShiftSwapRequests_Users_ResolvedByAdminId" FOREIGN KEY ("ResolvedByAdminId") REFERENCES "Users" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_ShiftSwapRequests_Users_TargetGuardId" FOREIGN KEY ("TargetGuardId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);


CREATE TABLE "IncidentNotes" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_IncidentNotes" PRIMARY KEY AUTOINCREMENT,
    "IncidentId" INTEGER NOT NULL,
    "Note" TEXT NOT NULL,
    "AddedById" INTEGER NOT NULL,
    "AddedAt" TEXT NOT NULL,
    CONSTRAINT "FK_IncidentNotes_Incidents_IncidentId" FOREIGN KEY ("IncidentId") REFERENCES "Incidents" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_IncidentNotes_Users_AddedById" FOREIGN KEY ("AddedById") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);


INSERT INTO "CampusZones" ("Id", "Description", "IsActive", "Name")
VALUES (1, 'Primary entrance for all visitors and vehicles', 1, 'Main Gate');
SELECT changes();

INSERT INTO "CampusZones" ("Id", "Description", "IsActive", "Name")
VALUES (2, 'Central library and study area', 1, 'Library Block');
SELECT changes();

INSERT INTO "CampusZones" ("Id", "Description", "IsActive", "Name")
VALUES (3, 'Staff and premium student parking', 1, 'Parking Lot A');
SELECT changes();



INSERT INTO "NotificationTemplates" ("Id", "BodyTemplate", "Channel", "EventType", "IsActive", "Subject")
VALUES (1, 'SECURITY ALERT: Blacklisted QR Code {{PersonName}} scanned at {{Location}} at {{Time}}.', 0, 0, 1, NULL);
SELECT changes();

INSERT INTO "NotificationTemplates" ("Id", "BodyTemplate", "Channel", "EventType", "IsActive", "Subject")
VALUES (2, 'A blacklisted person ({{PersonName}}) attempted entry at {{Location}} at {{Time}}.', 1, 0, 1, 'Campus Sentinel - Blacklist Alert');
SELECT changes();

INSERT INTO "NotificationTemplates" ("Id", "BodyTemplate", "Channel", "EventType", "IsActive", "Subject")
VALUES (3, 'CAPACITY WARNING: Campus occupancy has reached {{Occupancy}}% ({{CurrentCount}}/{{MaxCapacity}}).', 0, 1, 1, NULL);
SELECT changes();

INSERT INTO "NotificationTemplates" ("Id", "BodyTemplate", "Channel", "EventType", "IsActive", "Subject")
VALUES (4, 'AFTER-HOURS ACCESS: {{PersonName}} scanned at {{Location}} at {{Time}}.', 0, 2, 1, NULL);
SELECT changes();

INSERT INTO "NotificationTemplates" ("Id", "BodyTemplate", "Channel", "EventType", "IsActive", "Subject")
VALUES (5, 'NEW INCIDENT: A {{SeverityLevel}} incident ''{{Title}}'' was reported at {{Location}}.', 0, 3, 1, NULL);
SELECT changes();

INSERT INTO "NotificationTemplates" ("Id", "BodyTemplate", "Channel", "EventType", "IsActive", "Subject")
VALUES (6, (('A new {{SeverityLevel}} severity incident has been logged: {{Title}}' || CHAR(10)) || ('Location: {{Location}}' || (CHAR(10) || 'Time: {{Time}}'))), 1, 3, 1, 'Campus Sentinel - New Incident Reported');
SELECT changes();

INSERT INTO "NotificationTemplates" ("Id", "BodyTemplate", "Channel", "EventType", "IsActive", "Subject")
VALUES (7, 'MISSED SHIFT: Guard {{PersonName}} missed their scheduled shift at {{Location}} starting at {{Time}}.', 0, 4, 1, NULL);
SELECT changes();

INSERT INTO "NotificationTemplates" ("Id", "BodyTemplate", "Channel", "EventType", "IsActive", "Subject")
VALUES (8, 'UNAUTHORIZED SCAN: An invalid or unauthorized QR code was scanned at {{Location}} at {{Time}}.', 0, 5, 1, NULL);
SELECT changes();



INSERT INTO "SystemSettings" ("Id", "Description", "Key", "UpdatedAt", "Value")
VALUES (1, 'The display name of the campus', 'CampusName', '2026-01-01 00:00:00', 'Campus Sentinel Institute');
SELECT changes();

INSERT INTO "SystemSettings" ("Id", "Description", "Key", "UpdatedAt", "Value")
VALUES (2, 'If true, blocks all entry regardless of QR validity', 'LockdownMode', '2026-01-01 00:00:00', 'false');
SELECT changes();

INSERT INTO "SystemSettings" ("Id", "Description", "Key", "UpdatedAt", "Value")
VALUES (3, 'Maximum campus occupancy before warnings are issued', 'MaxCapacity', '2026-01-01 00:00:00', '5000');
SELECT changes();



INSERT INTO "Users" ("Id", "CreatedAt", "IsActive", "PasswordHash", "Role", "Username")
VALUES (1, '2026-01-01 00:00:00', 1, 'admin123', 'Admin', 'admin');
SELECT changes();

INSERT INTO "Users" ("Id", "CreatedAt", "IsActive", "PasswordHash", "Role", "Username")
VALUES (2, '2026-01-01 00:00:00', 1, 'hassan123', 'Admin', 'hassan');
SELECT changes();



CREATE INDEX "IX_AccessLogs_GuardId" ON "AccessLogs" ("GuardId");


CREATE INDEX "IX_BlacklistLogs_BlacklistedBy" ON "BlacklistLogs" ("BlacklistedBy");


CREATE INDEX "IX_Challans_IssuedByUserId" ON "Challans" ("IssuedByUserId");


CREATE INDEX "IX_Challans_QrCodeId" ON "Challans" ("QrCodeId");


CREATE INDEX "IX_Challans_QrCodeId_Status" ON "Challans" ("QrCodeId", "Status");


CREATE INDEX "IX_Challans_Status" ON "Challans" ("Status");


CREATE INDEX "IX_GuardShifts_CreatedByAdminId" ON "GuardShifts" ("CreatedByAdminId");


CREATE INDEX "IX_GuardShifts_GuardUserId" ON "GuardShifts" ("GuardUserId");


CREATE INDEX "IX_GuardShifts_ZoneId" ON "GuardShifts" ("ZoneId");


CREATE INDEX "IX_IncidentNotes_AddedById" ON "IncidentNotes" ("AddedById");


CREATE INDEX "IX_IncidentNotes_IncidentId" ON "IncidentNotes" ("IncidentId");


CREATE INDEX "IX_Incidents_LinkedAccessLogId" ON "Incidents" ("LinkedAccessLogId");


CREATE INDEX "IX_Incidents_ReportedById" ON "Incidents" ("ReportedById");


CREATE INDEX "IX_NotificationLogs_RecipientUserId" ON "NotificationLogs" ("RecipientUserId");


CREATE INDEX "IX_ShiftSwapRequests_RequestingGuardId" ON "ShiftSwapRequests" ("RequestingGuardId");


CREATE INDEX "IX_ShiftSwapRequests_ResolvedByAdminId" ON "ShiftSwapRequests" ("ResolvedByAdminId");


CREATE INDEX "IX_ShiftSwapRequests_ShiftId" ON "ShiftSwapRequests" ("ShiftId");


CREATE INDEX "IX_ShiftSwapRequests_TargetGuardId" ON "ShiftSwapRequests" ("TargetGuardId");


CREATE INDEX "IX_UserNotificationPreferences_UserId" ON "UserNotificationPreferences" ("UserId");



-- =========================================================================
-- TRIGGERS
-- =========================================================================

-- Trigger to automatically update 'UpdatedAt' column in SystemSettings
CREATE TRIGGER IF NOT EXISTS TRG_Update_SystemSettings_UpdatedAt
AFTER UPDATE ON SystemSettings
FOR EACH ROW
BEGIN
    UPDATE SystemSettings SET UpdatedAt = CURRENT_TIMESTAMP WHERE Id = NEW.Id;
END;

-- Trigger to prevent inserting a new shift in the past
CREATE TRIGGER IF NOT EXISTS TRG_Prevent_Past_GuardShifts
BEFORE INSERT ON GuardShifts
FOR EACH ROW
WHEN NEW.ShiftDate < date('now')
BEGIN
    SELECT RAISE(ABORT, 'Cannot schedule a shift in the past.');
END;

-- =========================================================================
-- CRUD FUNCTIONALITY (Examples / Templates)
-- =========================================================================

-- 1. CREATE (Insert)

-- Insert a new Student
-- INSERT INTO Students (QrCodeId, Session, FullName, Department, RegistrationNo, Gender, ResidencyType, IsBlacklisted, CreatedAt)
-- VALUES ('QR_STU_001', 2026, 'John Doe', 'Computer Science', 'CS-2026-001', 'Male', 'Hostelite', 0, CURRENT_TIMESTAMP);

-- Insert a new Incident
-- INSERT INTO Incidents (Title, Description, Severity, Status, Location, ReportedById, ReportedAt)
-- VALUES ('Suspicious Activity', 'Unidentified bag left near gate', 2, 0, 'Main Gate', 1, CURRENT_TIMESTAMP);


-- 2. READ (Select)

-- Get all Active Guards
-- SELECT * FROM Users WHERE Role = 'SecurityGuard' AND IsActive = 1;

-- Get all Pending Challans with User Info
-- SELECT c.Id, c.QrCodeId, c.SubjectName, c.ViolationType, c.Amount, c.IssueDate, u.Username AS IssuedBy
-- FROM Challans c
-- INNER JOIN Users u ON c.IssuedByUserId = u.Id
-- WHERE c.Status = 0;

-- Get daily Access Logs for a specific zone
-- SELECT * FROM AccessLogs WHERE GateLocation = 'Main Gate' AND Timestamp >= date('now');


-- 3. UPDATE

-- Update Challan Status to Paid (Status = 1)
-- UPDATE Challans 
-- SET Status = 1, Notes = 'Paid via online portal' 
-- WHERE Id = 1 AND Status = 0;

-- Update Student Blacklist Status
-- UPDATE Students 
-- SET IsBlacklisted = 1 
-- WHERE RegistrationNo = 'CS-2026-001';


-- 4. DELETE

-- Delete a specific Guard Shift
-- DELETE FROM GuardShifts WHERE Id = 10;

-- Delete a temporary visitor record after expiration
-- DELETE FROM Visitors WHERE ExpirationTime < CURRENT_TIMESTAMP;

