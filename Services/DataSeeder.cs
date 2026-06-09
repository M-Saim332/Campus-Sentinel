using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CampusSentinel.Data;
using CampusSentinel.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusSentinel.Services
{
    public class DataSeeder
    {
        private readonly ApplicationDbContext _context;

        public DataSeeder(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SeedAllAsync()
        {
            await SeedSecurityGuardsAsync();
            await SeedStudentsAsync();
            await SeedStaffAsync();
            await SeedVisitorsAsync();
            await SeedAccessLogsAsync();
            await SeedIncidentsAsync();
            await SeedZonesAsync();
            await SeedShiftsAsync();
            await SeedChallansAsync();
        }

        private async Task SeedSecurityGuardsAsync()
        {
            if (await _context.Users.AnyAsync(u => u.Role == "SecurityGuard")) return;

            for (int i = 1; i <= 10; i++)
            {
                _context.Users.Add(new SecurityGuard
                {
                    Username = $"guard{i}",
                    PasswordHash = "guard123",
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-i)
                });
            }
            await _context.SaveChangesAsync();
        }

        private async Task SeedStudentsAsync()
        {
            int currentCount = await _context.Students.CountAsync();
            if (currentCount >= 1100) return;

            string[] names = { "Ali Khan", "Sara Ahmed", "Zainab Malik", "Bilal Hassan", "Fatima Raza", "Usman Sheikh", "Ayesha Siddiqa", "Omer Farooq", "Maryam Bibi", "Hassan Ali" };
            string[] depts = { "CS", "EE", "BBA", "ME", "DS", "AI" };

            for (int i = currentCount; i < 1110; i++)
            {
                var dept = depts[i % depts.Length];
                _context.Students.Add(new Student
                {
                    FullName = names[i % names.Length] + " " + i,
<<<<<<< HEAD
                    RegistrationNo = $"STD-{2022 + (i % 3)}-{dept}-{1000 + i}",
                    QrCodeId = $"STD-{2022 + (i % 3)}-{dept}-{1000 + i}",
=======
                    RegistrationNo = $"{2022 + (i % 3)}-{dept}-{1000 + i}",
                    QrCodeId = $"{2022 + (i % 3)}-{dept}-{1000 + i}",
>>>>>>> 0536f83 (Update project)
                    Department = dept,
                    Gender = i % 2 == 0 ? "Male" : "Female",
                    ResidencyType = i % 3 == 0 ? "Hostelite" : "Day Scholar",
                    Session = 2022 + (i % 3),
                    CreatedAt = DateTime.Now.AddMonths(- (i % 12))
                });
            }
            await _context.SaveChangesAsync();
        }

        private async Task SeedStaffAsync()
        {
            int currentCount = await _context.Staff.CountAsync();
            if (currentCount >= 200) return;

            string[] names = { "Dr. Ahmed", "Ms. Sana", "Mr. Rashid", "Dr. Zafar", "Ms. Hira", "Mr. Kamran", "Ms. Nadia", "Dr. Iqbal", "Mr. Salman", "Ms. Rabia" };
            StaffCategory[] categories = { StaffCategory.Faculty, StaffCategory.Faculty, StaffCategory.Helper, StaffCategory.Faculty, StaffCategory.Gardener, StaffCategory.Worker, StaffCategory.Faculty, StaffCategory.Faculty, StaffCategory.Helper, StaffCategory.Faculty };

            for (int i = currentCount; i < 210; i++)
            {
                _context.Staff.Add(new Staff
                {
                    FullName = names[i % names.Length] + " " + i,
                    Category = categories[i % categories.Length],
                    Designation = categories[i % categories.Length] == StaffCategory.Faculty ? "Professor" : "Support Staff",
                    DepartmentOrUni = "Campus University",
                    Gender = i % 2 == 0 ? "Male" : "Female",
                    PhoneNumber = $"0300-123456{(i % 10)}",
                    StaffId = $"STF-{2000 + i}",
                    CreatedAt = DateTime.Now.AddMonths(-(i % 12))
                });
            }
            await _context.SaveChangesAsync();
        }

        private async Task SeedVisitorsAsync()
        {
            int currentCount = await _context.Visitors.CountAsync();
            if (currentCount >= 20) return;

            string[] names = { "Guest 1", "Guest 2", "Contractor A", "Delivery B", "Parent C", "Visitor D", "Guest E", "Contractor F", "Delivery G", "Parent H" };

            for (int i = currentCount; i < 30; i++)
            {
                _context.Visitors.Add(new Visitor
                {
                    FullName = names[i % names.Length] + " " + i,
                    Purpose = "Official Visit",
                    Role = i % 3 == 0 ? "Contractor" : "Guest",
                    TemporaryQrCodeId = $"VST-{10000 + i}",
                    ExpirationTime = DateTime.Now.AddHours(4),
                    CreatedAt = DateTime.Now.AddDays(-(i % 30))
                });
            }
            await _context.SaveChangesAsync();
        }

        private async Task SeedAccessLogsAsync()
        {
            if (await _context.AccessLogs.AnyAsync()) return;

            var guard = await _context.Users.FirstOrDefaultAsync(u => u.Role == "SecurityGuard");
            var students = await _context.Students.Take(5).ToListAsync();
            
            for (int i = 0; i < 10; i++)
            {
                var student = students[i % students.Count];
                _context.AccessLogs.Add(new AccessLog
                {
                    TargetId = student.QrCodeId,
                    TargetType = "Student",
                    Direction = i % 2 == 0 ? "Entry" : "Exit",
                    Status = "Granted",
                    Timestamp = DateTime.Now.AddHours(-i),
                    GateLocation = "Main Gate",
                    GuardId = guard?.Id,
                    Reason = "Routine check"
                });
            }
            await _context.SaveChangesAsync();
        }

        private async Task SeedIncidentsAsync()
        {
            int currentCount = await _context.Incidents.CountAsync();
            if (currentCount >= 30) return;

            var guard = await _context.Users.FirstOrDefaultAsync(u => u.Role == "SecurityGuard");
            string[] titles = { "Lost Item", "Unauthorized Access", "Power Failure", "Water Leak", "Noise Complaint", "Minor Scuffle", "Health Emergency", "Fire Drill", "Equipment Damage", "Suspicious Activity" };

            for (int i = currentCount; i < 40; i++)
            {
                _context.Incidents.Add(new Incident
                {
                    Title = titles[i % titles.Length],
                    Description = $"Automatically generated description for {titles[i % titles.Length]}",
                    Severity = (IncidentSeverity)(i % 4),
                    Status = (IncidentStatus)(i % 4),
                    Location = i % 2 == 0 ? "Library" : "Cafeteria",
                    ReportedById = guard?.Id ?? 1,
                    ReportedAt = DateTime.Now.AddDays(-(i % 30))
                });
            }
            await _context.SaveChangesAsync();
        }

        private async Task SeedZonesAsync()
        {
            if (await _context.CampusZones.AnyAsync()) return;

            string[] zones = { "Main Gate", "Library", "Hostel A", "Hostel B", "Cafeteria", "Admin Block", "Sports Complex" };
            foreach (var name in zones)
            {
                _context.CampusZones.Add(new CampusZone { Name = name, Description = $"Security monitoring for {name}" });
            }
            await _context.SaveChangesAsync();
        }

        private async Task SeedShiftsAsync()
        {
            if (await _context.GuardShifts.AnyAsync()) return;

            var guards = await _context.Users.Where(u => u.Role == "SecurityGuard").Take(5).ToListAsync();
            var zones = await _context.CampusZones.Take(3).ToListAsync();
            var admin = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Admin");
            var today = DateOnly.FromDateTime(DateTime.Today);

            if (!guards.Any() || !zones.Any() || admin == null) return;

            for (int d = 0; d < 7; d++) // Seed for the whole week
            {
                var shiftDate = today.AddDays(d - (int)today.DayOfWeek + 1); // Start from Monday
                for (int i = 0; i < 3; i++)
                {
                    _context.GuardShifts.Add(new GuardShift
                    {
                        GuardUserId = guards[i % guards.Count].Id,
                        ZoneId = zones[i % zones.Count].Id,
                        ShiftDate = shiftDate,
                        StartTime = new TimeOnly(8 + (i * 4), 0),
                        EndTime = new TimeOnly(12 + (i * 4), 0),
                        Status = ShiftStatus.Scheduled,
                        CreatedByAdminId = admin.Id
                    });
                }
            }
            await _context.SaveChangesAsync();
        }

        private async Task SeedChallansAsync()
        {
            int currentCount = await _context.Challans.CountAsync();
            if (currentCount >= 20) return;

            var admin = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Admin");
            if (admin == null) return;

            var students = await _context.Students.Take(50).ToListAsync();
            if (!students.Any()) return;
            string[] violations = { "No ID Card", "Parking Violation", "Smoking in Prohibited Area", "Late Entry", "Littering" };

            for (int i = currentCount; i < 30; i++)
            {
                var student = students[i % students.Count];
                _context.Challans.Add(new Challan
                {
                    QrCodeId = student.QrCodeId,
                    SubjectName = student.FullName,
                    SubjectType = "Student",
                    ViolationType = violations[i % violations.Length],
                    Amount = 500 + (i * 50),
                    IssuedByUserId = admin.Id,
                    IssueDate = DateTime.UtcNow.AddDays(- (i % 30)),
                    Status = i % 2 == 0 ? ChallanStatus.Pending : ChallanStatus.Paid
                });
            }
            await _context.SaveChangesAsync();
        }
    }
}
