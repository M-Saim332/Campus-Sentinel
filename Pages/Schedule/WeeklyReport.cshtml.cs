using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CampusSentinel.Models;
using CampusSentinel.Services;

namespace CampusSentinel.Pages.Schedule
{
    public class WeeklyReportModel : PageModel
    {
        private readonly ISchedulerService _schedulerService;

        public WeeklyReportModel(ISchedulerService schedulerService)
        {
            _schedulerService = schedulerService;
        }

        public DateOnly WeekStartDate { get; set; }
        public List<GuardShiftSummary> GuardSummaries { get; set; }

        public class GuardGuardSummary
        {
            public string GuardName { get; set; }
            public int Scheduled { get; set; }
            public int Completed { get; set; }
            public int Missed { get; set; }
        }

        // Renamed to avoid nested class name collision
        public class GuardShiftSummary
        {
            public string GuardName { get; set; }
            public int Scheduled { get; set; }
            public int Completed { get; set; }
            public int Missed { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string date)
        {
            if (string.IsNullOrEmpty(date) || !DateOnly.TryParse(date, out DateOnly parsedDate))
            {
                var today = DateOnly.FromDateTime(DateTime.Today);
                int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
                WeekStartDate = today.AddDays(-1 * diff);
            }
            else
            {
                WeekStartDate = parsedDate;
            }

            var shifts = await _schedulerService.GetWeeklyScheduleAsync(WeekStartDate);

            GuardSummaries = shifts
                .GroupBy(s => s.GuardUser?.Username ?? "Unknown")
                .Select(g => new GuardShiftSummary
                {
                    GuardName = g.Key,
                    Scheduled = g.Count(s => s.Status == ShiftStatus.Scheduled || s.Status == ShiftStatus.Active),
                    Completed = g.Count(s => s.Status == ShiftStatus.Completed),
                    Missed = g.Count(s => s.Status == ShiftStatus.Missed)
                })
                .ToList();

            return Page();
        }

        public async Task<IActionResult> OnGetDownloadPdfAsync(string date)
        {
            DateOnly weekStartDate;
            if (string.IsNullOrEmpty(date) || !DateOnly.TryParse(date, out weekStartDate))
            {
                var today = DateOnly.FromDateTime(DateTime.Today);
                int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
                weekStartDate = today.AddDays(-1 * diff);
            }

            try
            {
                var pdfBytes = await _schedulerService.GenerateWeeklyRosterPdfAsync(weekStartDate);
                return File(pdfBytes, "application/pdf", $"GuardRoster_{weekStartDate:yyyy-MM-dd}.pdf");
            }
            catch
            {
                TempData["ErrorMessage"] = "Failed to generate PDF report.";
                return RedirectToPage(new { date = weekStartDate.ToString("yyyy-MM-dd") });
            }
        }
    }
}
