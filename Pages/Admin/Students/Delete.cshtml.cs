using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CampusSentinel.Data;
using CampusSentinel.Models;

namespace CampusSentinel.Pages.Admin.Students
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Student Student { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            Student = await _context.Students.FirstOrDefaultAsync(m => m.Id == id);

            if (Student == null) return NotFound();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null) return NotFound();

            Student = await _context.Students.FindAsync(id);

            if (Student != null)
            {
                _context.Students.Remove(Student);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
