using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CampusSentinel.Models;
using CampusSentinel.Services;
using Microsoft.EntityFrameworkCore;
using CampusSentinel.Data;
using System.Linq;

namespace CampusSentinel.Pages.Incidents
{
    public class IndexModel : PageModel
    {
        private readonly IIncidentService _incidentService;
        private readonly ApplicationDbContext _context;

        public IndexModel(IIncidentService incidentService, ApplicationDbContext context)
        {
            _incidentService = incidentService;
            _context = context;
        }

        public List<Incident> Incidents { get; set; }
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public IncidentSeverity? FilterSeverity { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public IncidentStatus? FilterStatus { get; set; }

        public async Task OnGetAsync(int pageNumber = 1)
        {
            CurrentPage = pageNumber;
            int pageSize = 10;

            int? reportedByUserId = null;
            if (User.IsInRole("SecurityGuard"))
            {
                var username = User.Identity.Name;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user != null)
                {
                    reportedByUserId = user.Id;
                }
            }

            var result = await _incidentService.GetPagedIncidentsAsync(reportedByUserId, FilterSeverity, FilterStatus, CurrentPage, pageSize);
            Incidents = result.Items;
            TotalCount = result.TotalCount;
            TotalPages = (TotalCount + pageSize - 1) / pageSize;
        }
    }
}
