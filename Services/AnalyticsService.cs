using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CampusSentinel.Models;
using CampusSentinel.Data;
using Microsoft.EntityFrameworkCore;

namespace CampusSentinel.Services
{
    public interface IAnalyticsService
    {
        Task<int> GetCurrentOccupancyAsync();
        Task<List<(int Hour, int Count)>> GetPeakHoursAsync();
        Task<byte[]> GenerateWeeklyReportPdfAsync();
        Task<DashboardStats> GetDashboardStatsAsync();
    }

    public class DashboardStats
    {
        public int TotalStudents { get; set; }
        public int TotalVisitorsToday { get; set; }
        public int CurrentOccupancy { get; set; }
        public int BlacklistedCount { get; set; }
        public int OpenIncidentsToday { get; set; }
        public int ActiveGuardsCount { get; set; }

        public List<string> ActiveGuardsList { get; set; } = new List<string>();
        public int DeniedAccessCount { get; set; }
    }

    public class AnalyticsService : IAnalyticsService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPdfService _pdfService;

        public AnalyticsService(ApplicationDbContext context, IPdfService pdfService)
        {
            _context = context;
            _pdfService = pdfService;
        }

        public async Task<int> GetCurrentOccupancyAsync()
        {
            var today = DateTime.Today;
            
            var entriesToday = await _context.AccessLogs
                .Where(l => l.Timestamp >= today && l.Direction == "Entry" && l.Status == "Granted")
                .Select(l => l.TargetId)
                .ToListAsync();

            var exitsToday = await _context.AccessLogs
                .Where(l => l.Timestamp >= today && l.Direction == "Exit" && l.Status == "Granted")
                .Select(l => l.TargetId)
                .ToListAsync();

            var onCampus = entriesToday.Count - exitsToday.Count;
            return Math.Max(0, onCampus);
        }

        public async Task<List<(int Hour, int Count)>> GetPeakHoursAsync()
        {
            var today = DateTime.Today;
            var data = await _context.AccessLogs
                .Where(l => l.Timestamp >= today && l.Direction == "Entry" && l.Status == "Granted")
                .GroupBy(l => l.Timestamp.Hour)
                .Select(g => new { Hour = g.Key, Count = g.Count() })
                .OrderBy(x => x.Hour)
                .ToListAsync();

            return data.Select(x => (x.Hour, x.Count)).ToList();
        }

        public async Task<DashboardStats> GetDashboardStatsAsync()
        {
            var now = DateTime.Now;
            var today = DateOnly.FromDateTime(now);
            var timeNow = TimeOnly.FromDateTime(now);

            var activeGuards = await _context.GuardShifts
                .Include(s => s.GuardUser)
                .Where(s => s.ShiftDate == today && s.StartTime <= timeNow && s.EndTime >= timeNow && (s.Status == ShiftStatus.Active || s.Status == ShiftStatus.Scheduled))
                .Select(s => s.GuardUser.Username)
                .Distinct()
                .ToListAsync();

            return new DashboardStats
            {
                // Count denied access attempts for today
                DeniedAccessCount = await _context.AccessLogs.CountAsync(l => l.Status == "Denied" && l.Timestamp >= DateTime.Today),
                TotalStudents = await _context.Students.CountAsync(),
                // Count visitors created today (midnight to next midnight)
                TotalVisitorsToday = await _context.Visitors.CountAsync(v => v.CreatedAt >= DateTime.Today && v.CreatedAt < DateTime.Today.AddDays(1)),
                CurrentOccupancy = await GetCurrentOccupancyAsync(),
                BlacklistedCount = await _context.Students.CountAsync(s => s.IsBlacklisted) + await _context.Visitors.CountAsync(v => v.IsBlacklisted),
                OpenIncidentsToday = await _context.Incidents.CountAsync(i => i.Status == IncidentStatus.Open && i.ReportedAt >= DateTime.Today),
                ActiveGuardsCount = activeGuards.Count,
                ActiveGuardsList = activeGuards
            };
        }

        public async Task<byte[]> GenerateWeeklyReportPdfAsync()
        {
            var lastWeek = DateTime.Today.AddDays(-7);
            var logs = await _context.AccessLogs
                .Where(l => l.Timestamp >= lastWeek)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();

            return _pdfService.GenerateWeeklyReport(logs);
        }
    }
}
