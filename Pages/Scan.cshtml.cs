using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using CampusSentinel.Services;

namespace CampusSentinel.Pages
{
    [Authorize(Policy = "RequireGuardRole")]
    public class ScanModel : PageModel
    {
        private readonly IVerificationService _verificationService;

        public ScanModel(IVerificationService verificationService)
        {
            _verificationService = verificationService;
        }

        [BindProperty]
        [Required]
        public string QrCodeId { get; set; }

        [BindProperty]
        [Required]
        public string Direction { get; set; } = "Entry"; // Default to Entry

        public string ScanResult { get; set; }
        public bool IsGranted { get; set; }
        public bool ShowResult { get; set; }
        public int? CurrentLogId { get; set; }

        public void OnGet()
        {
            ShowResult = false;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Hardcoded gate location for demonstration
            int? currentGuardId = null; 
            string gateLocation = "Main Gate";

            var result = await _verificationService.VerifyAccessAsync(QrCodeId, Direction, currentGuardId, gateLocation);
            
            IsGranted = result.IsGranted;
            ScanResult = result.Reason;
            CurrentLogId = result.LogId;
            ShowResult = true;

            return Page();
        }
    }
}
