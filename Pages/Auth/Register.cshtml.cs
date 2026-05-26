using System.ComponentModel.DataAnnotations;
 using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CampusSentinel.Models;
using CampusSentinel.Services;

namespace CampusSentinel.Pages.Auth
{
    public class RegisterModel : PageModel
    {
        private readonly IAuthService _authService;

        public RegisterModel(IAuthService authService)
        {
            _authService = authService;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>Set to true when a duplicate username is detected — triggers the popup.</summary>
        public bool ShowDuplicateWarning { get; set; } = false;

        public class InputModel
        {
            [Required(ErrorMessage = "Username is required.")]
            [StringLength(30, MinimumLength = 4, ErrorMessage = "Username must be between 4 and 30 characters.")]
            [RegularExpression(@"^[a-zA-Z0-9_\.]+$", ErrorMessage = "Username may only contain letters, numbers, underscores or dots.")]
            public string Username { get; set; }

            [Required(ErrorMessage = "Password is required.")]
            [StringLength(16, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 16 characters.")]
            [RegularExpression(
                @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,16}$",
                ErrorMessage = "Password must be 8–16 characters and include at least one uppercase letter, one lowercase letter, one number, and one special character (@, #, $, !, %, *, ?, &, etc.).")]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Required]
            public string Role { get; set; } // "Admin" or "SecurityGuard"
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            CampusSentinel.Models.User newUser;
            if (Input.Role == "Admin")
                newUser = new CampusSentinel.Models.Admin    { Username = Input.Username };
            else
                newUser = new CampusSentinel.Models.SecurityGuard { Username = Input.Username };

            var result = await _authService.SignUpAsync(newUser, Input.Password);

            if (result == null)
            {
                // Username already exists — show warning popup instead of crashing
                ShowDuplicateWarning = true;
                ModelState.AddModelError(string.Empty,
                    $"An account with the username \"{Input.Username}\" already exists. Please choose a different username.");
                return Page();
            }

            return RedirectToPage("/Auth/Login");
        }
    }
}
