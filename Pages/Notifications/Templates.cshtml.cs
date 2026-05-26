using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CampusSentinel.Models;
using CampusSentinel.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace CampusSentinel.Pages.Notifications
{
    [Authorize(Policy = "RequireAdminRole")]
    public class TemplatesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public TemplatesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<NotificationTemplate> Templates { get; set; }

        [BindProperty]
        public NotificationTemplate NewTemplate { get; set; }

        public async Task OnGetAsync()
        {
            Templates = await _context.NotificationTemplates
                .OrderBy(t => t.EventType)
                .ThenBy(t => t.Channel)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostToggleAsync(int id)
        {
            var template = await _context.NotificationTemplates.FindAsync(id);
            if (template != null)
            {
                template.IsActive = !template.IsActive;
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var template = await _context.NotificationTemplates.FindAsync(id);
            if (template != null)
            {
                _context.NotificationTemplates.Remove(template);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAddAsync()
        {
            if (!ModelState.IsValid) return Page();

            _context.NotificationTemplates.Add(NewTemplate);
            await _context.SaveChangesAsync();
            return RedirectToPage();
        }
    }
}
