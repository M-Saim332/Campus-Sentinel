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
    public class MyScheduleModel : PageModel
    {
        private readonly ISchedulerService _schedulerService;
        private readonly ApplicationDbContext _context;

        public MyScheduleModel(ISchedulerService schedulerService, ApplicationDbContext context)
        {
            _schedulerService = schedulerService;
            _context = context;
        }

        public DateOnly WeekStartDate { get; set; }
        public List<GuardShift> Shifts { get; set; }
        
        [BindProperty]
        public int ShiftIdToSwap { get; set; }

        [BindProperty]
        [Required]
        public int TargetGuardId { get; set; }

        [BindProperty]
        [Required]
        public string SwapReason { get; set; }

        public List<User> AvailableGuards { get; set; }

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

            var username = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            
            if (user == null) return Unauthorized();

            Shifts = await _schedulerService.GetGuardScheduleAsync(user.Id, WeekStartDate);
            
            AvailableGuards = await _context.Users
                .Where(u => u.Role == "SecurityGuard" && u.Id != user.Id)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostSwapAsync()
        {
            var username = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return Unauthorized();

            if (!ModelState.IsValid)
            {
                return await OnGetAsync(WeekStartDate.ToString("yyyy-MM-dd"));
            }

            try
            {
                await _schedulerService.RequestSwapAsync(user.Id, TargetGuardId, ShiftIdToSwap, SwapReason);
                TempData["SuccessMessage"] = "Swap request submitted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage(new { date = WeekStartDate.ToString("yyyy-MM-dd") });
        }
        
        public async Task<IActionResult> OnPostMarkActiveAsync(int id)
        {
            var shift = await _context.GuardShifts.FindAsync(id);
            if(shift != null && shift.Status == ShiftStatus.Scheduled)
            {
                shift.Status = ShiftStatus.Active;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Shift marked as Active.";
            }
            return RedirectToPage(new { date = WeekStartDate.ToString("yyyy-MM-dd") });
        }
        
        public async Task<IActionResult> OnPostMarkCompletedAsync(int id)
        {
            await _schedulerService.CompleteShiftAsync(id);
            TempData["SuccessMessage"] = "Shift marked as Completed.";
            return RedirectToPage(new { date = WeekStartDate.ToString("yyyy-MM-dd") });
        }
    }
}
