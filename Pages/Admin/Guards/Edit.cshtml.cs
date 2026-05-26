using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CampusSentinel.Data;
using CampusSentinel.Models;

namespace CampusSentinel.Pages.Admin.Guards
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public SecurityGuard Guard { get; set; }

        [BindProperty]
        public string NewPassword { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Guard = await _context.SecurityGuards.FirstOrDefaultAsync(m => m.Id == id);

            if (Guard == null)
            {
                return NotFound();
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var guardToUpdate = await _context.SecurityGuards.FirstOrDefaultAsync(g => g.Id == Guard.Id);
            if (guardToUpdate == null)
            {
                return NotFound();
            }

            guardToUpdate.Username = Guard.Username;
            guardToUpdate.IsActive = Guard.IsActive;

            if (!string.IsNullOrWhiteSpace(NewPassword))
            {
                guardToUpdate.PasswordHash = NewPassword; // Match existing plain text auth logic
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!GuardExists(Guard.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        private bool GuardExists(int id)
        {
            return _context.SecurityGuards.Any(e => e.Id == id);
        }
    }
}
