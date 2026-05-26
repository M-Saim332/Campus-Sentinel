using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CampusSentinel.Data;
using CampusSentinel.Models;

namespace CampusSentinel.Pages.Admin.Guards
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            public string Username { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == Input.Username);
            if (existingUser != null)
            {
                ModelState.AddModelError("Input.Username", "This username is already taken. Please choose another.");
                return Page();
            }

            var guard = new SecurityGuard
            {
                Username = Input.Username,
                PasswordHash = Input.Password, // In a real app, hash this
                Role = "SecurityGuard"
            };

            _context.SecurityGuards.Add(guard);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
