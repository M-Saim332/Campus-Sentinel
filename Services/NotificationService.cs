using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CampusSentinel.Models;
using CampusSentinel.Data;
using Microsoft.EntityFrameworkCore;

namespace CampusSentinel.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly ISmsSender _smsSender;

        public NotificationService(ApplicationDbContext context, IEmailSender emailSender, ISmsSender smsSender)
        {
            _context = context;
            _emailSender = emailSender;
            _smsSender = smsSender;
        }

        public async Task SendAsync(NotificationEventType eventType, int recipientUserId, Dictionary<string, string> tokens, int? relatedEntityId = null, string relatedEntityType = null)
        {
            var user = await _context.Users.FindAsync(recipientUserId);
            if (user == null) return;

            var preferences = await _context.UserNotificationPreferences
                .FirstOrDefaultAsync(p => p.UserId == recipientUserId && p.EventType == eventType);

            // Default preferences if not set
            bool inApp = true;
            bool email = preferences?.EmailEnabled ?? false;
            bool sms = preferences?.SmsEnabled ?? false;

            var templates = await _context.NotificationTemplates
                .Where(t => t.EventType == eventType && t.IsActive)
                .ToListAsync();

            foreach (var template in templates)
            {
                bool shouldSend = template.Channel switch
                {
                    NotificationChannel.InApp => inApp,
                    NotificationChannel.Email => email,
                    NotificationChannel.SMS => sms,
                    _ => false
                };

                if (!shouldSend) continue;

                string renderedBody = RenderTemplate(template.BodyTemplate, tokens);
                string subject = RenderTemplate(template.Subject ?? "", tokens);

                var log = new NotificationLog
                {
                    EventType = eventType,
                    Channel = template.Channel,
                    RecipientUserId = recipientUserId,
                    RenderedBody = renderedBody,
                    Status = NotificationStatus.Pending,
                    ErrorMessage = string.Empty,
                    RelatedEntityId = relatedEntityId,
                    RelatedEntityType = relatedEntityType,
                    SentAt = DateTime.Now
                };

                try
                {
                    if (template.Channel == NotificationChannel.Email)
                    {
                        // Logic to get email address (assuming Username is email or add Email property to User)
                        // For now, assume Username is email or use a placeholder if not present.
                        // Ideally the User model should have an Email property.
                        string targetEmail = user.Username.Contains("@") ? user.Username : "user@example.com";
                        log.RecipientContact = targetEmail;
                        await _emailSender.SendEmailAsync(targetEmail, subject, renderedBody);
                    }
                    else if (template.Channel == NotificationChannel.SMS)
                    {
                        string targetPhone = preferences?.PhoneNumber ?? "000-000-0000";
                        log.RecipientContact = targetPhone;
                        await _smsSender.SendSmsAsync(targetPhone, renderedBody);
                    }
                    
                    log.Status = NotificationStatus.Sent;
                }
                catch (Exception ex)
                {
                    log.Status = NotificationStatus.Failed;
                    log.ErrorMessage = ex.Message;
                }

                _context.NotificationLogs.Add(log);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
        {
            var log = await _context.NotificationLogs.FindAsync(notificationId);
            if (log == null || log.RecipientUserId != userId) return false;

            log.IsRead = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task MarkAllAsReadAsync(int userId)
        {
            var logs = await _context.NotificationLogs
                .Where(l => l.RecipientUserId == userId && !l.IsRead && l.Channel == NotificationChannel.InApp)
                .ToListAsync();

            foreach (var log in logs)
            {
                log.IsRead = true;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _context.NotificationLogs
                .CountAsync(l => l.RecipientUserId == userId && !l.IsRead && l.Channel == NotificationChannel.InApp);
        }

        public async Task<(List<NotificationLog> Items, int TotalCount)> GetInboxAsync(int userId, NotificationEventType? eventType = null, bool unreadOnly = false, int page = 1, int pageSize = 10)
        {
            var query = _context.NotificationLogs
                .Where(l => l.RecipientUserId == userId && l.Channel == NotificationChannel.InApp);

            if (eventType.HasValue)
                query = query.Where(l => l.EventType == eventType.Value);

            if (unreadOnly)
                query = query.Where(l => !l.IsRead);

            int totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(l => l.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<List<NotificationLog>> GetRecentInAppAsync(int userId, int count = 5)
        {
            return await _context.NotificationLogs
                .Where(l => l.RecipientUserId == userId && l.Channel == NotificationChannel.InApp)
                .OrderByDescending(l => l.SentAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<bool> ReSendAsync(int notificationLogId)
        {
            var log = await _context.NotificationLogs
                .Include(l => l.RecipientUser)
                .FirstOrDefaultAsync(l => l.Id == notificationLogId);

            if (log == null || log.Status == NotificationStatus.Sent) return false;

            try
            {
                if (log.Channel == NotificationChannel.Email)
                {
                    await _emailSender.SendEmailAsync(log.RecipientContact, "Re-send: Notification", log.RenderedBody);
                }
                else if (log.Channel == NotificationChannel.SMS)
                {
                    await _smsSender.SendSmsAsync(log.RecipientContact, log.RenderedBody);
                }
                log.Status = NotificationStatus.Sent;
                log.ErrorMessage = string.Empty;
                log.SentAt = DateTime.Now;
            }
            catch (Exception ex)
            {
                log.ErrorMessage = ex.Message;
                return false;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        private string RenderTemplate(string template, Dictionary<string, string> tokens)
        {
            string result = template;
            foreach (var token in tokens)
            {
                result = result.Replace("{{" + token.Key + "}}", token.Value);
            }
            return result;
        }
    }
}
