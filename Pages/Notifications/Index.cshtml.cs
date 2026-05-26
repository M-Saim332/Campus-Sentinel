using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CampusSentinel.Models;
using CampusSentinel.Services;
using CampusSentinel.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace CampusSentinel.Pages.Notifications
{
    public class IndexModel : PageModel
    {
        private readonly INotificationService _notificationService;
        private readonly ApplicationDbContext _context;

        public IndexModel(INotificationService notificationService, ApplicationDbContext context)
        {
            _notificationService = notificationService;
            _context = context;
        }

        public List<NotificationLog> Notifications { get; set; }
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

        [BindProperty(SupportsGet = true)]
        public NotificationEventType? FilterEventType { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool UnreadOnly { get; set; }

        public async Task OnGetAsync(int pageNumber = 1)
        {
            var username = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return;

            CurrentPage = pageNumber;
            var result = await _notificationService.GetInboxAsync(user.Id, FilterEventType, UnreadOnly, pageNumber, 10);
            Notifications = result.Items;
            TotalCount = result.TotalCount;
            TotalPages = (int)System.Math.Ceiling(TotalCount / 10.0);
        }

        public async Task<IActionResult> OnPostMarkAsReadAsync(int id)
        {
            var username = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return Unauthorized();

            await _notificationService.MarkAsReadAsync(id, user.Id);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostMarkAllAsReadAsync()
        {
            var username = User.Identity.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return Unauthorized();

            await _notificationService.MarkAllAsReadAsync(user.Id);
            return RedirectToPage();
        }
    }
}
