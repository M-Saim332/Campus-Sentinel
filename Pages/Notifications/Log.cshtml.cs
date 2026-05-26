using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CampusSentinel.Models;
using CampusSentinel.Data;
using CampusSentinel.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace CampusSentinel.Pages.Notifications
{
    [Authorize(Policy = "RequireAdminRole")]
    public class LogModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public LogModel(ApplicationDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public List<NotificationLog> Logs { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public NotificationChannel? FilterChannel { get; set; }

        [BindProperty(SupportsGet = true)]
        public NotificationStatus? FilterStatus { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.NotificationLogs
                .Include(l => l.RecipientUser)
                .AsQueryable();

            if (FilterChannel.HasValue)
                query = query.Where(l => l.Channel == FilterChannel.Value);

            if (FilterStatus.HasValue)
                query = query.Where(l => l.Status == FilterStatus.Value);

            Logs = await query
                .OrderByDescending(l => l.SentAt)
                .Take(100)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostResendAsync(int id)
        {
            await _notificationService.ReSendAsync(id);
            return RedirectToPage();
        }
    }
}
