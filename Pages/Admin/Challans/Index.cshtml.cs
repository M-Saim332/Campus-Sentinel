using System.Collections.Generic;
using System.Threading.Tasks;
using CampusSentinel.Models;
using CampusSentinel.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CampusSentinel.Pages.Admin.Challans
{
    /// <summary>
    /// Page model for the Challans index — lists all challans with
    /// optional filter by status and inline status-update actions.
    /// </summary>
    public class IndexModel : PageModel
    {
        private readonly IChallanService _challanService;

        public IndexModel(IChallanService challanService)
        {
            _challanService = challanService;
        }

        /// <summary>All challans matching the current filter, ordered newest-first.</summary>
        public IEnumerable<Challan> Challans { get; set; } = [];

        /// <summary>The currently-selected status filter, or null for "All".</summary>
        [BindProperty(SupportsGet = true)]
        public ChallanStatus? FilterStatus { get; set; }

        /// <summary>Inline success/error feedback after a status update.</summary>
        public string? StatusMessage { get; set; }
        public bool    IsStatusError { get; set; }

        public async Task OnGetAsync()
        {
            var all = await _challanService.GetAllChallansAsync();

            // Apply status filter if one is selected
            Challans = FilterStatus.HasValue
                ? System.Linq.Enumerable.Where(all, c => c.Status == FilterStatus.Value)
                : all;
        }

        /// <summary>
        /// Handles inline status update (e.g. marking a challan Paid / Cancelled)
        /// posted from the action buttons on the index table.
        /// </summary>
        public async Task<IActionResult> OnPostUpdateStatusAsync(int challanId, ChallanStatus newStatus, string? notes)
        {
            var success = await _challanService.UpdateStatusAsync(challanId, newStatus, notes);

            if (success)
            {
                StatusMessage = $"Challan #{challanId} status updated to {newStatus}.";
                IsStatusError = false;
            }
            else
            {
                StatusMessage = $"Challan #{challanId} not found.";
                IsStatusError = true;
            }

            // Reload the list
            var all = await _challanService.GetAllChallansAsync();
            Challans = FilterStatus.HasValue
                ? System.Linq.Enumerable.Where(all, c => c.Status == FilterStatus.Value)
                : all;

            return Page();
        }
    }
}
