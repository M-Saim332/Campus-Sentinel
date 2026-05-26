using System.ComponentModel.DataAnnotations;

namespace CampusSentinel.Models
{
    public class NotificationTemplate
    {
        [Key]
        public int Id { get; set; }

        public NotificationEventType EventType { get; set; }

        public NotificationChannel Channel { get; set; }

        [MaxLength(200)]
        public string? Subject { get; set; } // For Email

        [Required]
        public string BodyTemplate { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
