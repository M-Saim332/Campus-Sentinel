using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CampusSentinel.Models;
using CampusSentinel.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace CampusSentinel.Pages.Notifications
{
    public class PreferencesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public PreferencesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public List<PreferenceInputModel> Preferences { get; set; }

        [BindProperty]
        public string PhoneNumber { get; set; }

        public class PreferenceInputModel
        {
            public NotificationEventType EventType { get; set; }
            public bool InApp { get; set; }
            public bool Email { get; set; }
            public bool Sms { get; set; }
        }

        public async Task OnGetAsync()
        {
            var username = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return;

            var existingPrefs = await _context.UserNotificationPreferences
                .Where(p => p.UserId == user.Id)
                .ToListAsync();

            PhoneNumber = existingPrefs.FirstOrDefault()?.PhoneNumber;

            Preferences = System.Enum.GetValues<NotificationEventType>().Select(e => {
                var pref = existingPrefs.FirstOrDefault(p => p.EventType == e);
                return new PreferenceInputModel
                {
                    EventType = e,
                    InApp = pref?.InAppEnabled ?? true,
                    Email = pref?.EmailEnabled ?? false,
                    Sms = pref?.SmsEnabled ?? false
                };
            }).ToList();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var username = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return Unauthorized();

            foreach (var input in Preferences)
            {
                var pref = await _context.UserNotificationPreferences
                    .FirstOrDefaultAsync(p => p.UserId == user.Id && p.EventType == input.EventType);

                if (pref == null)
                {
                    pref = new UserNotificationPreference
                    {
                        UserId = user.Id,
                        EventType = input.EventType
                    };
                    _context.UserNotificationPreferences.Add(pref);
                }

                pref.InAppEnabled = input.InApp;
                pref.EmailEnabled = input.Email;
                pref.SmsEnabled = input.Sms;
                pref.PhoneNumber = PhoneNumber;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Notification preferences saved.";
            return RedirectToPage();
        }
    }
}
