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
    public class EditModel : PageModel
    {
        private readonly IIncidentService _incidentService;
        private readonly ApplicationDbContext _context;

        public EditModel(IIncidentService incidentService, ApplicationDbContext context)
        {
            _incidentService = incidentService;
            _context = context;
        }

        [BindProperty]
        public IncidentInputModel Input { get; set; }

        public class IncidentInputModel
        {
            public int Id { get; set; }

            [Required]
            [MaxLength(150)]
            public string Title { get; set; }

            [Required]
            public string Description { get; set; }

            [Required]
            public IncidentSeverity Severity { get; set; }

            [Required]
            public IncidentStatus Status { get; set; }

            [Required]
            [MaxLength(100)]
            public string Location { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var incident = await _incidentService.GetIncidentByIdAsync(id);
            if (incident == null) return NotFound();

            Input = new IncidentInputModel
            {
                Id = incident.Id,
                Title = incident.Title,
                Description = incident.Description,
                Severity = incident.Severity,
                Status = incident.Status,
                Location = incident.Location
            };

            return Page();
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
                Id = Input.Id,
                Title = Input.Title,
                Description = Input.Description,
                Severity = Input.Severity,
                Status = Input.Status,
                Location = Input.Location
            };

            await _incidentService.UpdateIncidentAsync(incident, user.Id);

            TempData["SuccessMessage"] = "Incident updated successfully.";
            return RedirectToPage("./Detail", new { id = incident.Id });
        }
    }
}
