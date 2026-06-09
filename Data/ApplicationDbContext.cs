using System;
using Microsoft.EntityFrameworkCore;
using CampusSentinel.Models;

namespace CampusSentinel.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<SecurityGuard> SecurityGuards { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Visitor> Visitors { get; set; }
        public DbSet<Staff> Staff { get; set; }
        public DbSet<AccessLog> AccessLogs { get; set; }
        public DbSet<BlacklistLog> BlacklistLogs { get; set; }
        public DbSet<Incident> Incidents { get; set; }
        public DbSet<IncidentNote> IncidentNotes { get; set; }
        public DbSet<CampusZone> CampusZones { get; set; }
        public DbSet<GuardShift> GuardShifts { get; set; }
        public DbSet<ShiftSwapRequest> ShiftSwapRequests { get; set; }
        public DbSet<NotificationTemplate> NotificationTemplates { get; set; }
        public DbSet<NotificationLog> NotificationLogs { get; set; }
        public DbSet<UserNotificationPreference> UserNotificationPreferences { get; set; }

        // ── Challan Generation Feature ────────────────────────────────────────
        public DbSet<Challan> Challans { get; set; }

        // ── System Configuration Feature ──────────────────────────────────────
        public DbSet<SystemSetting> SystemSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // TPH (Table-Per-Hierarchy) for Users
            modelBuilder.Entity<User>()
                .HasDiscriminator<string>("Role")
                .HasValue<Admin>("Admin")
                .HasValue<SecurityGuard>("SecurityGuard");

            modelBuilder.Entity<Incident>()
                .HasOne(i => i.ReportedBy)
                .WithMany()
                .HasForeignKey(i => i.ReportedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<IncidentNote>()
                .HasOne(n => n.AddedBy)
                .WithMany()
                .HasForeignKey(n => n.AddedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GuardShift>()
                .HasOne(s => s.GuardUser)
                .WithMany()
                .HasForeignKey(s => s.GuardUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GuardShift>()
                .HasOne(s => s.CreatedByAdmin)
                .WithMany()
                .HasForeignKey(s => s.CreatedByAdminId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ShiftSwapRequest>()
                .HasOne(r => r.RequestingGuard)
                .WithMany()
                .HasForeignKey(r => r.RequestingGuardId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ShiftSwapRequest>()
                .HasOne(r => r.TargetGuard)
                .WithMany()
                .HasForeignKey(r => r.TargetGuardId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ShiftSwapRequest>()
                .HasOne(r => r.ResolvedByAdmin)
                .WithMany()
                .HasForeignKey(r => r.ResolvedByAdminId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<NotificationLog>()
                .HasOne(l => l.RecipientUser)
                .WithMany()
                .HasForeignKey(l => l.RecipientUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserNotificationPreference>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Challan entity configuration ──────────────────────────────────
            // FK: Challan → Users (the official who issued the fine).
            // Restrict delete so removing a user account cannot silently orphan
            // transactional challan records.
            modelBuilder.Entity<Challan>()
                .HasOne(c => c.IssuedBy)
                .WithMany()
                .HasForeignKey(c => c.IssuedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Index on QrCodeId for fast per-person challan lookups.
            modelBuilder.Entity<Challan>()
                .HasIndex(c => c.QrCodeId)
                .HasDatabaseName("IX_Challans_QrCodeId");

            // Index on Status for fast dashboard / report queries (e.g. all Pending).
            modelBuilder.Entity<Challan>()
                .HasIndex(c => c.Status)
                .HasDatabaseName("IX_Challans_Status");

            // Combined index for the common filter: QR code AND status together.
            modelBuilder.Entity<Challan>()
                .HasIndex(c => new { c.QrCodeId, c.Status })
                .HasDatabaseName("IX_Challans_QrCodeId_Status");

            // Seed CampusZones
            modelBuilder.Entity<CampusZone>().HasData(
                new CampusZone { Id = 1, Name = "Main Gate", Description = "Primary entrance for all visitors and vehicles", IsActive = true },
                new CampusZone { Id = 2, Name = "Library Block", Description = "Central library and study area", IsActive = true },
                new CampusZone { Id = 3, Name = "Parking Lot A", Description = "Staff and premium student parking", IsActive = true }
            );

            // Seed Notification Templates
            modelBuilder.Entity<NotificationTemplate>().HasData(
                new NotificationTemplate { Id = 1, EventType = NotificationEventType.BlacklistAlert, Channel = NotificationChannel.InApp, BodyTemplate = "SECURITY ALERT: Blacklisted QR Code {{PersonName}} scanned at {{Location}} at {{Time}}.", IsActive = true },
                new NotificationTemplate { Id = 2, EventType = NotificationEventType.BlacklistAlert, Channel = NotificationChannel.Email, Subject = "Campus Sentinel - Blacklist Alert", BodyTemplate = "A blacklisted person ({{PersonName}}) attempted entry at {{Location}} at {{Time}}.", IsActive = true },
                new NotificationTemplate { Id = 3, EventType = NotificationEventType.CapacityThreshold, Channel = NotificationChannel.InApp, BodyTemplate = "CAPACITY WARNING: Campus occupancy has reached {{Occupancy}}% ({{CurrentCount}}/{{MaxCapacity}}).", IsActive = true },
                new NotificationTemplate { Id = 4, EventType = NotificationEventType.AfterHoursAccess, Channel = NotificationChannel.InApp, BodyTemplate = "AFTER-HOURS ACCESS: {{PersonName}} scanned at {{Location}} at {{Time}}.", IsActive = true },
                new NotificationTemplate { Id = 5, EventType = NotificationEventType.IncidentLogged, Channel = NotificationChannel.InApp, BodyTemplate = "NEW INCIDENT: A {{SeverityLevel}} incident '{{Title}}' was reported at {{Location}}.", IsActive = true },
                new NotificationTemplate { Id = 6, EventType = NotificationEventType.IncidentLogged, Channel = NotificationChannel.Email, Subject = "Campus Sentinel - New Incident Reported", BodyTemplate = "A new {{SeverityLevel}} severity incident has been logged: {{Title}}\nLocation: {{Location}}\nTime: {{Time}}", IsActive = true },
                new NotificationTemplate { Id = 7, EventType = NotificationEventType.ShiftMissed, Channel = NotificationChannel.InApp, BodyTemplate = "MISSED SHIFT: Guard {{PersonName}} missed their scheduled shift at {{Location}} starting at {{Time}}.", IsActive = true },
                new NotificationTemplate { Id = 8, EventType = NotificationEventType.UnauthorizedScan, Channel = NotificationChannel.InApp, BodyTemplate = "UNAUTHORIZED SCAN: An invalid or unauthorized QR code was scanned at {{Location}} at {{Time}}.", IsActive = true }
            );

            // Seed System Settings
            var staticDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            modelBuilder.Entity<SystemSetting>().HasData(
                new SystemSetting { Id = 1, Key = "CampusName", Value = "Campus Sentinel Institute", Description = "The display name of the campus", UpdatedAt = staticDate },
                new SystemSetting { Id = 2, Key = "LockdownMode", Value = "false", Description = "If true, blocks all entry regardless of QR validity", UpdatedAt = staticDate },
                new SystemSetting { Id = 3, Key = "MaxCapacity", Value = "5000", Description = "Maximum campus occupancy before warnings are issued", UpdatedAt = staticDate }
            );

            // ── Seed Default Administrator ──
            modelBuilder.Entity<Admin>().HasData(
                new Admin 
                { 
                    Id = 1, 
                    Username = "admin", 
                    PasswordHash = "admin123", // In production this must be hashed
                    IsActive = true,
                    CreatedAt = staticDate
                },
                new Admin 
                { 
                    Id = 2, 
                    Username = "hassan", 
                    PasswordHash = "hassan123", 
                    IsActive = true,
                    CreatedAt = staticDate
                }
            );

            // Configure tables that have database triggers (prevents OUTPUT clause errors)
            modelBuilder.Entity<GuardShift>()
                .ToTable(tb => tb.HasTrigger("trg_GuardShifts_PreventOverlap"));

            modelBuilder.Entity<Challan>()
                .ToTable(tb => tb.HasTrigger("trg_Challans_AutoBlacklist"));
        }
    }
}
