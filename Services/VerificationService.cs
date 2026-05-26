using System;
using System.Threading.Tasks;
using CampusSentinel.Models;
using CampusSentinel.Repositories;
using Microsoft.EntityFrameworkCore;
using CampusSentinel.Data;
using System.Linq;

namespace CampusSentinel.Services
{
    public class VerificationService : IVerificationService
    {
        private readonly IPersonRepository _personRepository;
        private readonly IRepository<AccessLog> _accessLogRepository;
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _config;

        public VerificationService(
            IPersonRepository personRepository, 
            IRepository<AccessLog> accessLogRepository, 
            ApplicationDbContext context,
            INotificationService notificationService,
            Microsoft.Extensions.Configuration.IConfiguration config)
        {
            _personRepository = personRepository;
            _accessLogRepository = accessLogRepository;
            _context = context;
            _notificationService = notificationService;
            _config = config;
        }

        public async Task<(bool IsGranted, string Reason, int LogId)> VerifyAccessAsync(string qrcode, string direction, int? guardId, string gateLocation)
        {
            string targetType = "Unknown";
            bool isGranted = false;
            string reason = "Invalid QR Code";
            
            var student = await _personRepository.GetStudentByQrCodeAsync(qrcode);
            var visitor = await _personRepository.GetVisitorByTemporaryQrCodeAsync(qrcode);
            var staff   = await _context.Staff.FirstOrDefaultAsync(s => s.StaffId == qrcode);

            if (student != null)
            {
                targetType = "Student";
                if (student.IsBlacklisted)
                {
                    reason = "Blacklisted";
                }
                else if (string.Equals(direction, "Entry", StringComparison.OrdinalIgnoreCase))
                {
                    var oneHourAgo = DateTime.Now.AddHours(-1);
                    var recentEntry = await _context.AccessLogs
                        .Where(l => l.TargetId == qrcode && l.Direction == "Entry" && l.Status == "Granted" && l.Timestamp >= oneHourAgo)
                        .OrderByDescending(l => l.Timestamp)
                        .FirstOrDefaultAsync();

                    if (recentEntry != null)
                    {
                        var cooldownRemaining = recentEntry.Timestamp.AddHours(1) - DateTime.Now;
                        int mins = (int)Math.Ceiling(cooldownRemaining.TotalMinutes);
                        if (mins <= 0) mins = 1;
                        reason = $"Cannot re-enter within 1 hour (cooldown: {mins} mins left)";
                    }
                    else
                    {
                        isGranted = true;
                        reason = "Access Granted";
                    }
                }
                else
                {
                    isGranted = true;
                    reason = "Access Granted";
                }
            }
            else if (visitor != null)
            {
                targetType = "Visitor";
                if (visitor.IsBlacklisted) reason = "Blacklisted";
                else if (visitor.ExpirationTime < DateTime.Now) reason = "Pass Expired";
                else { isGranted = true; reason = "Access Granted"; }
            }
            else if (staff != null)
            {
                targetType = "Staff";
                if (staff.IsBlacklisted) reason = "Blacklisted";
                else { isGranted = true; reason = "Access Granted"; }
            }

            var log = new AccessLog
            {
                TargetId = qrcode,
                TargetType = targetType,
                Status = isGranted ? "Granted" : "Denied",
                Direction = string.IsNullOrEmpty(direction) ? "Entry" : direction,
                Reason = reason,
                GuardId = guardId,
                GateLocation = gateLocation,
                Timestamp = DateTime.Now
            };

            await _accessLogRepository.AddAsync(log);
            await _accessLogRepository.SaveChangesAsync();

            // Trigger Notifications
            await TriggerNotificationsAsync(log, qrcode, targetType, isGranted, reason);

            return (isGranted, reason, log.Id);
        }

        private async Task TriggerNotificationsAsync(AccessLog log, string qrcode, string targetType, bool isGranted, string reason)
        {
            var tokens = new Dictionary<string, string>
            {
                { "Location", log.GateLocation },
                { "Time", log.Timestamp.ToString("HH:mm:ss") },
                { "PersonName", targetType != "Unknown" ? qrcode : "Unknown" }
            };

            var admins = await _context.Admins.ToListAsync();

            // 1. Blacklist Alert
            if (reason == "Blacklisted")
            {
                foreach (var admin in admins)
                {
                    await _notificationService.SendAsync(NotificationEventType.BlacklistAlert, admin.Id, tokens, log.Id, "AccessLog");
                }
            }

            // 2. Unauthorized Scan (Invalid QR Code)
            if (targetType == "Unknown")
            {
                foreach (var admin in admins)
                {
                    await _notificationService.SendAsync(NotificationEventType.UnauthorizedScan, admin.Id, tokens, log.Id, "AccessLog");
                }
            }

            // 3. After-Hours Access
            var startStr = _config["Notifications:AfterHoursStart"] ?? "22:00";
            var endStr = _config["Notifications:AfterHoursEnd"] ?? "06:00";
            var nowTime = TimeOnly.FromDateTime(log.Timestamp);
            
            if (TimeOnly.TryParse(startStr, out var afterStart) && TimeOnly.TryParse(endStr, out var afterEnd))
            {
                // Simple check for overnight range
                bool isAfterHours = false;
                if (afterStart > afterEnd) // e.g. 22:00 to 06:00
                {
                    isAfterHours = nowTime >= afterStart || nowTime <= afterEnd;
                }
                else // e.g. 00:00 to 06:00
                {
                    isAfterHours = nowTime >= afterStart && nowTime <= afterEnd;
                }

                if (isAfterHours && isGranted)
                {
                    foreach (var admin in admins)
                    {
                        await _notificationService.SendAsync(NotificationEventType.AfterHoursAccess, admin.Id, tokens, log.Id, "AccessLog");
                    }
                }
            }

            // 4. Capacity Threshold
            int capacityLimit = int.Parse(_config["Notifications:CampusCapacityLimit"] ?? "500");
            int thresholdPercent = int.Parse(_config["Notifications:CapacityAlertThresholdPercent"] ?? "80");
            
            // Simplified occupancy check (in a real app, use a cached count or optimized query)
            int currentCount = await _context.AccessLogs
                .Where(l => l.Status == "Granted")
                .GroupBy(l => l.TargetId)
                .Select(g => g.OrderByDescending(l => l.Timestamp).First())
                .CountAsync(l => l.Direction == "Entry");

            double occupancyPercent = (double)currentCount / capacityLimit * 100;
            if (occupancyPercent >= thresholdPercent)
            {
                tokens["Occupancy"] = occupancyPercent.ToString("F1");
                tokens["CurrentCount"] = currentCount.ToString();
                tokens["MaxCapacity"] = capacityLimit.ToString();
                
                foreach (var admin in admins)
                {
                    await _notificationService.SendAsync(NotificationEventType.CapacityThreshold, admin.Id, tokens, log.Id, "AccessLog");
                }
            }
        }

        public async Task<bool> CheckBlacklistAsync(string qrcode)
        {
            var student = await _personRepository.GetStudentByQrCodeAsync(qrcode);
            if (student != null && student.IsBlacklisted) return true;

            var visitor = await _personRepository.GetVisitorByTemporaryQrCodeAsync(qrcode);
            if (visitor != null && visitor.IsBlacklisted) return true;

            var staff = await _context.Staff.AnyAsync(s => s.StaffId == qrcode && s.IsBlacklisted);
            if (staff) return true;

            return false;
        }

        public async Task<Visitor> GenerateVisitorPassAsync(Visitor visitorInfo)
        {
            return visitorInfo;
        }
    }
}
