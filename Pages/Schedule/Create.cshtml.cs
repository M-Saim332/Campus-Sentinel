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
using System.ComponentModel.DataAnnotations;

namespace CampusSentinel.Pages.Schedule
{
    public class CreateModel : PageModel
    {
        private readonly ISchedulerService _schedulerService;
        private readonly ApplicationDbContext _context;

        public CreateModel(ISchedulerService schedulerService, ApplicationDbContext context)
        {
            _schedulerService = schedulerService;
            _context = context;
        }

        [BindProperty]
        public ShiftInputModel Input { get; set; }

        public class ShiftInputModel
        {
            [Required]
            public int GuardUserId { get; set; }

            [Required]
            public int ZoneId { get; set; }

            [Required]
            public DateOnly ShiftDate { get; set; }

            [Required]
            public TimeOnly StartTime { get; set; }

            [Required]
            public TimeOnly EndTime { get; set; }
        }

        public List<User> Guards { get; set; }
        public List<CampusZone> Zones { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            Guards = await _context.Users.Where(u => u.Role == "SecurityGuard").ToListAsync();
            Zones = await _context.CampusZones.Where(z => z.IsActive).ToListAsync();
            
            Input = new ShiftInputModel
            {
                ShiftDate = DateOnly.FromDateTime(DateTime.Today),
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(16, 0)
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                Guards = await _context.Users.Where(u => u.Role == "SecurityGuard").ToListAsync();
                Zones = await _context.CampusZones.Where(z => z.IsActive).ToListAsync();
                return Page();
            }

            var username = User.Identity.Name;
            var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (adminUser == null) return Unauthorized();

            // Night shifts are allowed: e.g. StartTime=22:00, EndTime=05:00 means shift crosses midnight.
            // No time-order validation needed here.

            try
            {
                await _schedulerService.AssignShiftAsync(Input.GuardUserId, Input.ZoneId, Input.ShiftDate, Input.StartTime, Input.EndTime, adminUser.Id);
                TempData["SuccessMessage"] = "Shift assigned successfully.";
                return RedirectToPage("./Index", new { date = Input.ShiftDate.ToString("yyyy-MM-dd") });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                Guards = await _context.Users.Where(u => u.Role == "SecurityGuard").ToListAsync();
                Zones = await _context.CampusZones.Where(z => z.IsActive).ToListAsync();
                return Page();
            }
        }
    }
}
