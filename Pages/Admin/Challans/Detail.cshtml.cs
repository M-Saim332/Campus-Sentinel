using System.Threading.Tasks;
using CampusSentinel.Models;
using CampusSentinel.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CampusSentinel.Pages.Admin.Challans
{
    /// <summary>
    /// Page model for the Challan detail view.
    /// Allows an Admin to review all fields of a specific challan
    /// and update its status (e.g. mark Paid, Disputed, or Cancelled).
    /// </summary>
    public class DetailModel : PageModel
    {
        private readonly IChallanService _challanService;

        public DetailModel(IChallanService challanService)
        {
            _challanService = challanService;
        }

        /// <summary>The challan being viewed.</summary>
        public Challan? Challan { get; set; }

        /// <summary>Feedback message shown after a status update.</summary>
        public string? StatusMessage { get; set; }
        public bool    IsStatusError { get; set; }

        // ── GET ──────────────────────────────────────────────────────────────

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Challan = await _challanService.GetChallanByIdAsync(id);

            if (Challan == null)
                return NotFound();

            return Page();
        }

        // ── POST: update status ───────────────────────────────────────────────

        /// <summary>
        /// Handles the status-update form on the detail page.
        /// Validates inputs, delegates to the service, and re-renders the page
        /// with an inline success or error message.
        /// </summary>
        public async Task<IActionResult> OnPostUpdateStatusAsync(
            int id,
            ChallanStatus newStatus,
            string? notes)
        {
            // Re-load the challan so the page can be re-rendered on failure
            Challan = await _challanService.GetChallanByIdAsync(id);
            if (Challan == null)
                return NotFound();

            var success = await _challanService.UpdateStatusAsync(id, newStatus, notes);

            if (success)
            {
                StatusMessage = $"Status updated to {newStatus} successfully.";
                IsStatusError = false;
                // Reload to reflect changes
                Challan = await _challanService.GetChallanByIdAsync(id);
            }
            else
            {
                StatusMessage = "Failed to update status. Please try again.";
                IsStatusError = true;
            }

            return Page();
        }
    }
}
