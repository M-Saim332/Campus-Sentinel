using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CampusSentinel.Models;
using CampusSentinel.Services;
using CampusSentinel.Data;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace CampusSentinel.Pages.Incidents
{
    public class DetailModel : PageModel
    {
        private readonly IIncidentService _incidentService;
        private readonly ApplicationDbContext _context;

        public DetailModel(IIncidentService incidentService, ApplicationDbContext context)
        {
            _incidentService = incidentService;
            _context = context;
        }

        public Incident Incident { get; set; }

        [BindProperty]
        [Required]
        public string NewNote { get; set; }

        [BindProperty]
        public IncidentStatus NewStatus { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Incident = await _incidentService.GetIncidentByIdAsync(id);
            if (Incident == null) return NotFound();

            NewStatus = Incident.Status;

            // Ensure guards only see their own
            if (User.IsInRole("SecurityGuard"))
            {
                var username = User.Identity.Name;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null || Incident.ReportedById != user.Id)
                {
                    return Forbid();
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAddNoteAsync(int id)
        {
            if (!string.IsNullOrWhiteSpace(NewNote))
            {
                var username = User.Identity.Name;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user != null)
                {
                    await _incidentService.AddNoteAsync(id, NewNote, user.Id);
                    TempData["SuccessMessage"] = "Note added successfully.";
                }
            }

            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(int id)
        {
            if (!User.IsInRole("Admin")) return Forbid();

            var username = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user != null)
            {
                await _incidentService.UpdateStatusAsync(id, NewStatus, user.Id);
                TempData["SuccessMessage"] = "Status updated successfully.";
            }

            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnGetDownloadPdfAsync(int id)
        {
            if (!User.IsInRole("Admin")) return Forbid();

            try
            {
                var pdfBytes = await _incidentService.GenerateIncidentPdfAsync(id);
                return File(pdfBytes, "application/pdf", $"Incident_Report_{id:D5}.pdf");
            }
            catch
            {
                TempData["ErrorMessage"] = "Failed to generate PDF.";
                return RedirectToPage(new { id });
            }
        }
    }
}
