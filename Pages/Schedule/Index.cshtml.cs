using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CampusSentinel.Models;
using CampusSentinel.Services;
using CampusSentinel.Data;
using Microsoft.EntityFrameworkCore;

namespace CampusSentinel.Pages.Schedule
{
    public class IndexModel : PageModel
    {
        private readonly ISchedulerService _schedulerService;
        private readonly ApplicationDbContext _context;

        public IndexModel(ISchedulerService schedulerService, ApplicationDbContext context)
        {
            _schedulerService = schedulerService;
            _context = context;
        }

        public DateOnly WeekStartDate { get; set; }
        public List<CampusZone> Zones { get; set; }
        public List<GuardShift> Shifts { get; set; }

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

            Zones = await _context.CampusZones.Where(z => z.IsActive).ToListAsync();
            Shifts = await _schedulerService.GetWeeklyScheduleAsync(WeekStartDate);

            return Page();
        }
    }
}
