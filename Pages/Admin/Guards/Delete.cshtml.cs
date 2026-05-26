using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CampusSentinel.Data;
using CampusSentinel.Models;

namespace CampusSentinel.Pages.Admin.Guards
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public SecurityGuard Guard { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            Guard = await _context.SecurityGuards.FirstOrDefaultAsync(m => m.Id == id);

            if (Guard == null) return NotFound();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null) return NotFound();

            Guard = await _context.SecurityGuards.FindAsync(id);

            if (Guard != null)
            {
                _context.SecurityGuards.Remove(Guard);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
