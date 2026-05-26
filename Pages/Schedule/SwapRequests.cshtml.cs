using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CampusSentinel.Models;
using CampusSentinel.Services;
using CampusSentinel.Data;
using Microsoft.EntityFrameworkCore;

namespace CampusSentinel.Pages.Schedule
{
    public class SwapRequestsModel : PageModel
    {
        private readonly ISchedulerService _schedulerService;
        private readonly ApplicationDbContext _context;

        public SwapRequestsModel(ISchedulerService schedulerService, ApplicationDbContext context)
        {
            _schedulerService = schedulerService;
            _context = context;
        }

        public List<ShiftSwapRequest> Requests { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            Requests = await _schedulerService.GetPendingSwapRequestsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostResolveAsync(int id, bool approve)
        {
            var username = User.Identity.Name;
            var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (adminUser == null) return Unauthorized();

            try
            {
                var success = await _schedulerService.ResolveSwapAsync(id, approve, adminUser.Id);
                if (success)
                {
                    TempData["SuccessMessage"] = approve ? "Swap request approved." : "Swap request rejected.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Could not resolve request. It may no longer be pending.";
                }
            }
            catch (System.Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage();
        }
    }
}
