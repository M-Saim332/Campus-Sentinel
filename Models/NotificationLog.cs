using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusSentinel.Models
{
    public class NotificationLog
    {
        [Key]
        public int Id { get; set; }

        public NotificationEventType EventType { get; set; }

        public NotificationChannel Channel { get; set; }

        public int RecipientUserId { get; set; }

        [ForeignKey("RecipientUserId")]
        public User RecipientUser { get; set; }

        [MaxLength(255)]
        public string? RecipientContact { get; set; } // Email or Phone

        [Required]
        public string RenderedBody { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.Now;

        public NotificationStatus Status { get; set; }

        public string? ErrorMessage { get; set; }

        public int? RelatedEntityId { get; set; }

        [MaxLength(100)]
        public string? RelatedEntityType { get; set; }

        public bool IsRead { get; set; } = false;
    }
}
