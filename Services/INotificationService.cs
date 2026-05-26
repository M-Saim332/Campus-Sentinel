using System.Collections.Generic;
using System.Threading.Tasks;
using CampusSentinel.Models;

namespace CampusSentinel.Services
{
    public interface INotificationService
    {
        Task SendAsync(NotificationEventType eventType, int recipientUserId, Dictionary<string, string> tokens, int? relatedEntityId = null, string relatedEntityType = null);
        Task<bool> MarkAsReadAsync(int notificationId, int userId);
        Task<int> GetUnreadCountAsync(int userId);
        Task<(List<NotificationLog> Items, int TotalCount)> GetInboxAsync(int userId, NotificationEventType? eventType = null, bool unreadOnly = false, int page = 1, int pageSize = 10);
        Task<bool> ReSendAsync(int notificationLogId);
        Task<List<NotificationLog>> GetRecentInAppAsync(int userId, int count = 5);
        Task MarkAllAsReadAsync(int userId);
    }
}
