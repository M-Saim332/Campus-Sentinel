-- Campus Sentinel Database Schema

-- Users Table (TPH)
CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    Role NVARCHAR(20) NOT NULL, -- Admin, SecurityGuard
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME DEFAULT GETDATE()
);

-- Students Table
CREATE TABLE Students (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    QrCodeId NVARCHAR(50) NOT NULL UNIQUE,
    Session INT NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    Department NVARCHAR(20) NOT NULL,
    RegistrationNo NVARCHAR(50) NOT NULL UNIQUE,
    Gender NVARCHAR(10) NOT NULL DEFAULT 'Male',
    ResidencyType NVARCHAR(20) NOT NULL DEFAULT 'Day Scholar',
    IsBlacklisted BIT DEFAULT 0,
    CreatedAt DATETIME DEFAULT GETDATE()
);

-- Visitors Table
CREATE TABLE Visitors (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    TemporaryQrCodeId NVARCHAR(50) NOT NULL UNIQUE,
    FullName NVARCHAR(100) NOT NULL,
    Purpose NVARCHAR(255),
    Role NVARCHAR(50) NOT NULL DEFAULT 'Guest',
    ExpirationTime DATETIME NOT NULL,
    IsBlacklisted BIT DEFAULT 0,
    CreatedAt DATETIME DEFAULT GETDATE()
);

-- Staff Table (Faculty, Helpers, Gardeners, etc.)
CREATE TABLE Staff (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    StaffId NVARCHAR(50) NOT NULL UNIQUE,
    Category INT NOT NULL, -- 0: Faculty, 1: Helper, 2: Gardener, 3: Worker
    FullName NVARCHAR(100) NOT NULL,
    Designation NVARCHAR(50) NOT NULL,
    DepartmentOrUni NVARCHAR(50) NOT NULL,
    Gender NVARCHAR(10) NOT NULL,
    PhoneNumber NVARCHAR(20),
    IsBlacklisted BIT DEFAULT 0,
    CreatedAt DATETIME DEFAULT GETDATE()
);

-- Access Logs Table
CREATE TABLE AccessLogs (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    TargetId NVARCHAR(50) NOT NULL, -- QrCodeId or TemporaryQrCodeId or StaffId
    TargetType NVARCHAR(20) NOT NULL, -- Student, Visitor, Staff
    Timestamp DATETIME DEFAULT GETDATE(),
    Status NVARCHAR(20) NOT NULL, -- Granted, Denied
    Direction NVARCHAR(10) NOT NULL, -- Entry, Exit
    Reason NVARCHAR(255),
    GuardId INT,
    GateLocation NVARCHAR(50),
    CONSTRAINT FK_AccessLogs_Users FOREIGN KEY (GuardId) REFERENCES Users(Id)
);

-- Blacklist Logs Table
CREATE TABLE BlacklistLogs (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PersonId NVARCHAR(50) NOT NULL,
    Reason NVARCHAR(MAX) NOT NULL,
    BlacklistedAt DATETIME DEFAULT GETDATE(),
    BlacklistedBy NVARCHAR(100) NOT NULL
);
