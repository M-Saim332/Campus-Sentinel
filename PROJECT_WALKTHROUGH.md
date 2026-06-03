# Campus Sentinel - Project Walkthrough

## 1. Project Overview
**Campus Sentinel** is a comprehensive campus security and access management system built using **ASP.NET Core Razor Pages** and **Entity Framework Core (EF Core)** with a **SQLite** database. It provides an all-in-one solution for managing campus security, tracking people entering/leaving, handling incidents, scheduling guards, and issuing challans (fines) for violations.

The system is designed with Role-Based Access Control (RBAC), distinguishing between Administrators (who configure the system and oversee operations) and Security Guards (who perform daily tasks such as logging incidents and monitoring access).

## 2. Core Modules and Features

### 2.1. Authentication & Authorization
The application uses Cookie-Based Authentication.
*   **Roles:** `Admin` and `SecurityGuard`.
*   **Access Control:** The main management area (`/Admin`) is accessible to both roles (mostly), but specific actions like creating, editing, and deleting users are restricted to `Admin` only. 
*   **Endpoints:** Pages are protected using standard ASP.NET Core conventions in `Program.cs`.

### 2.2. Identity Management (TPH Architecture)
The project uses Table-Per-Hierarchy (TPH) for the `User` model, with a discriminator column `Role`.
*   **Admin:** Inherits from `User`. Has full access.
*   **SecurityGuard:** Inherits from `User`. Associated with shifts.
*   **Subjects:** The system tracks `Students`, `Staff`, and `Visitors`. All of these subjects have a `QrCodeId` (or `TemporaryQrCodeId` for visitors) which is used as their primary identifier for access control and challan issuance.

### 2.3. Challan Module (Fines/Violations)
Allows authorized personnel to issue fines/challans by scanning a QR code.
*   **Issuance:** Scanned QR codes are automatically resolved to a Student, Staff, or Visitor.
*   **Data Integrity:** Restrict delete cascading ensures that deleting a user account does not orphan challan records.
*   **Status Tracking:** Challans can be tracked as `Pending`, `Paid`, `Disputed`, or `Cancelled`.

### 2.4. Security Incidents
Allows guards to log security incidents around the campus.
*   **Incident Logging:** Records the title, description, severity level, location, and the guard who reported it.
*   **Incident Notes:** Administrators and other guards can add supplementary notes/updates to an ongoing incident investigation.

### 2.5. Access & Blacklist Management
*   **Access Logs:** Tracks successful or failed entry/exit attempts at different campus zones (e.g., Main Gate, Library Block).
*   **Blacklist Logs:** Logs unauthorized attempts by blacklisted individuals.

### 2.6. Guard Scheduling & Shift Swapping
*   **Guard Shifts:** Admins can assign guards to specific shifts and zones.
*   **Shift Swapping:** Guards can request to swap shifts with other guards, which must then be resolved (approved/rejected) by an Admin.

### 2.7. Notification System
*   **Templates & Channels:** Uses customizable `NotificationTemplate` objects for different events (e.g., Blacklist Alert, Incident Logged) over various channels (Email, InApp).
*   **Preferences:** Users can configure their notification preferences (`UserNotificationPreference`).
*   **Logging:** All sent notifications are logged into `NotificationLog`.

## 3. Technology Stack & Architecture
*   **Framework:** ASP.NET Core (.NET 9 or equivalent, based on C# 13 features)
*   **UI Paradigm:** Razor Pages for a server-side rendered, traditional multi-page application structure.
*   **Database:** SQLite (`campussentinel.db`), providing a self-contained, serverless zero-configuration database, great for local deployments.
*   **ORM:** Entity Framework Core, utilizing fluent API configurations for Table-Per-Hierarchy and index creation.
*   **Dependency Injection:** Follows SOLID principles with explicit service and repository layers (e.g., `IRepository<T>`, `IChallanService`, `IAuthService`).
*   **Database Seeding:** Automatically seeds required schemas, indexes, static data (Zones, Templates, Settings), and default Admin users on startup.

## 4. Getting Started & Running the Project
1. Open a terminal in the project root directory.
2. Ensure you have the .NET SDK installed.
3. The database is configured to auto-create and seed data upon launch. 
4. Run the application:
   ```bash
   dotnet run
   ```
5. Navigate to the application URL in your browser. Use the default seeded Admin credentials to log in:
   * **Username:** `admin` or `hassan`
   * **Password:** `admin123` or `hassan123`

## 5. Directory Structure Overview
*   **`/Data`**: Contains the EF Core `ApplicationDbContext`.
*   **`/Models`**: Contains the POCO domain models representing the database schema.
*   **`/Pages`**: Contains the Razor Pages UI, organized by features (`/Admin`, `/Auth`, `/Incidents`, etc.).
*   **`/Repositories`**: Contains generic and specific repository implementations for data access.
*   **`/Services`**: Contains business logic encapsulated in separate services (ChallanService, AuthService, Email/SMS providers).
*   **`Program.cs`**: Main application configuration, DI registration, and pipeline setup.
