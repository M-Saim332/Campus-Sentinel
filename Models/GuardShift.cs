using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusSentinel.Models
{
    public class GuardShift
    {
        [Key]
        public int Id { get; set; }

        public int GuardUserId { get; set; }

        [ForeignKey("GuardUserId")]
        public User GuardUser { get; set; }

        public int ZoneId { get; set; }

        [ForeignKey("ZoneId")]
        public CampusZone Zone { get; set; }

        public DateOnly ShiftDate { get; set; }
        
        public TimeOnly StartTime { get; set; }
        
        public TimeOnly EndTime { get; set; }

        public ShiftStatus Status { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public int CreatedByAdminId { get; set; }

        [ForeignKey("CreatedByAdminId")]
        public User CreatedByAdmin { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
