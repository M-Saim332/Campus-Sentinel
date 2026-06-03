using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CampusSentinel.Data;
using CampusSentinel.Models;

namespace CampusSentinel.Pages.Settings
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public IndexModel(ApplicationDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        public string CampusName { get; set; }

        [BindProperty]
        public bool LockdownMode { get; set; }

        [BindProperty]
        public int MaxCapacity { get; set; }

        [BindProperty]
        public bool EmailNotifications { get; set; }

        [BindProperty]
        public bool InAppNotifications { get; set; }

        [BindProperty]
        public string CurrentPassword { get; set; }

        [BindProperty]
        public string NewPassword { get; set; }

        [BindProperty]
        public string ConfirmPassword { get; set; }

        public string ActiveTab { get; set; } = "system";
        public string StatusMessage { get; set; }
        public bool IsStatusError { get; set; }

        public async Task<IActionResult> OnGetAsync(string tab = "system")
        {
            ActiveTab = tab;

            // Load System Settings
            var campusNameSetting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == "CampusName");
            CampusName = campusNameSetting?.Value ?? "Campus Sentinel";

            var lockdownSetting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == "LockdownMode");
            LockdownMode = bool.TryParse(lockdownSetting?.Value, out var lm) && lm;

            var maxCapacitySetting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == "MaxCapacity");
            MaxCapacity = int.TryParse(maxCapacitySetting?.Value, out var mc) ? mc : 5000;

            // Load User Preferences
            var currentUser = await _db.Users.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            if (currentUser != null)
            {
                var pref = await _db.UserNotificationPreferences.FirstOrDefaultAsync(p => p.UserId == currentUser.Id);
                if (pref != null)
                {
                    EmailNotifications = pref.EmailEnabled;
                    InAppNotifications = pref.InAppEnabled;
                }
                else
                {
                    InAppNotifications = true;
                    EmailNotifications = false;
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostSaveSystemSettingsAsync()
        {
            if (!User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var settings = await _db.SystemSettings.ToListAsync();

            var cnSetting = settings.FirstOrDefault(s => s.Key == "CampusName");
            if (cnSetting != null) cnSetting.Value = CampusName;

            var lmSetting = settings.FirstOrDefault(s => s.Key == "LockdownMode");
            if (lmSetting != null) lmSetting.Value = LockdownMode.ToString().ToLower();

            var mcSetting = settings.FirstOrDefault(s => s.Key == "MaxCapacity");
            if (mcSetting != null) mcSetting.Value = MaxCapacity.ToString();

            await _db.SaveChangesAsync();

            StatusMessage = "System settings updated successfully.";
            IsStatusError = false;
            return await OnGetAsync("system");
        }

        public async Task<IActionResult> OnPostSavePreferencesAsync()
        {
            var currentUser = await _db.Users.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            if (currentUser != null)
            {
                var pref = await _db.UserNotificationPreferences.FirstOrDefaultAsync(p => p.UserId == currentUser.Id);
                if (pref == null)
                {
                    pref = new UserNotificationPreference
                    {
                        UserId = currentUser.Id,
                        EventType = NotificationEventType.UnauthorizedScan // Default
                    };
                    _db.UserNotificationPreferences.Add(pref);
                }

                pref.EmailEnabled = EmailNotifications;
                pref.InAppEnabled = InAppNotifications;

                await _db.SaveChangesAsync();
                StatusMessage = "Notification preferences saved.";
                IsStatusError = false;
            }

            return await OnGetAsync("preferences");
        }

        public async Task<IActionResult> OnPostChangePasswordAsync()
        {
            if (string.IsNullOrEmpty(CurrentPassword) || string.IsNullOrEmpty(NewPassword) || string.IsNullOrEmpty(ConfirmPassword))
            {
                StatusMessage = "Please fill all password fields.";
                IsStatusError = true;
                return await OnGetAsync("security");
            }

            if (NewPassword != ConfirmPassword)
            {
                StatusMessage = "New password and confirmation do not match.";
                IsStatusError = true;
                return await OnGetAsync("security");
            }

            var currentUser = await _db.Users.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            if (currentUser != null)
            {
                // Simple password check since this is a demo app (admin123)
                if (currentUser.PasswordHash != CurrentPassword)
                {
                    StatusMessage = "Incorrect current password.";
                    IsStatusError = true;
                    return await OnGetAsync("security");
                }

                currentUser.PasswordHash = NewPassword;
                await _db.SaveChangesAsync();

                StatusMessage = "Password updated successfully.";
                IsStatusError = false;
            }

            return await OnGetAsync("security");
        }
    }
}
