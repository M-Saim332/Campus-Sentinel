using System;
using System.ComponentModel.DataAnnotations;

namespace CampusSentinel.Models
{
    public class SystemSetting
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Key { get; set; }

        public string Value { get; set; }

        [MaxLength(255)]
        public string Description { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
