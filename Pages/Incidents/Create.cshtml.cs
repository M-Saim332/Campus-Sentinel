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
    public class CreateModel : PageModel
    {
        private readonly IIncidentService _incidentService;
        private readonly ApplicationDbContext _context;

        public CreateModel(IIncidentService incidentService, ApplicationDbContext context)
        {
            _incidentService = incidentService;
            _context = context;
        }

        [BindProperty]
        public IncidentInputModel Input { get; set; }

        public class IncidentInputModel
        {
            [Required]
            [MaxLength(150)]
            public string Title { get; set; }

            [Required]
            public string Description { get; set; }

            [Required]
            public IncidentSeverity Severity { get; set; }

            [Required]
            [MaxLength(100)]
            public string Location { get; set; }
            
            public int? LinkedAccessLogId { get; set; }
        }

        public void OnGet(int? logId)
        {
            Input = new IncidentInputModel
            {
                LinkedAccessLogId = logId
            };
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var username = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return Unauthorized();

            var incident = new Incident
            {
                Title = Input.Title,
                Description = Input.Description,
                Severity = Input.Severity,
                Location = Input.Location,
                ReportedById = user.Id,
                LinkedAccessLogId = Input.LinkedAccessLogId
            };

            await _incidentService.CreateIncidentAsync(incident);

            TempData["SuccessMessage"] = "Incident reported successfully.";
            return RedirectToPage("./Index");
        }
    }
}
