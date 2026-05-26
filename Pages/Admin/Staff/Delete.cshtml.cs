using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CampusSentinel.Data;
using CampusSentinel.Models;

namespace CampusSentinel.Pages.Admin.Staff
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public CampusSentinel.Models.Staff Staff { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Staff = await _context.Staff.FindAsync(id);
            if (Staff == null) return NotFound();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var staff = await _context.Staff.FindAsync(id);
            if (staff != null)
            {
                _context.Staff.Remove(staff);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage("./Index");
        }
    }
}
