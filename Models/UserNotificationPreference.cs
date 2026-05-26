using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusSentinel.Models
{
    public class UserNotificationPreference
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }

        public NotificationEventType EventType { get; set; }

        public bool InAppEnabled { get; set; } = true;

        public bool EmailEnabled { get; set; } = false;

        public bool SmsEnabled { get; set; } = false;

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }
    }
}
