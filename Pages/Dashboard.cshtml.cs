using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CampusSentinel.Models;
using CampusSentinel.Data;
using CampusSentinel.Services;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace CampusSentinel.Pages
{
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IAnalyticsService _analyticsService;

        public DashboardModel(ApplicationDbContext context, IAnalyticsService analyticsService)
        {
            _context = context;
            _analyticsService = analyticsService;
            Stats = new DashboardStats();
            RecentLogs = new List<AccessLog>();
            PeakHoursData = new List<int>();
        }

        public DashboardStats Stats { get; set; }
        public List<AccessLog> RecentLogs { get; set; }
        public List<int> PeakHoursData { get; set; }

        public async Task OnGetAsync()
        {
            Stats = await _analyticsService.GetDashboardStatsAsync();
            
            RecentLogs = await _context.AccessLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(10)
                .ToListAsync();

            var peakHours = await _analyticsService.GetPeakHoursAsync();
            // Prepare 24h data for chart
            PeakHoursData = Enumerable.Range(0, 24)
                .Select(h => peakHours.FirstOrDefault(p => p.Hour == h).Count)
                .ToList();
        }

        public async Task<IActionResult> OnGetOccupancyAsync()
        {
            int occupancy = await _analyticsService.GetCurrentOccupancyAsync();
            return new JsonResult(new { occupancy });
        }

        public async Task<IActionResult> OnGetDownloadReportAsync()
        {
            var pdfBytes = await _analyticsService.GenerateWeeklyReportPdfAsync();
            return File(pdfBytes, "application/pdf", $"Campus_Performance_Report_{DateTime.Now:yyyyMMdd}.pdf");
        }
    }
}
