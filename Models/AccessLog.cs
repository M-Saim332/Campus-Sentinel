using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusSentinel.Models
{
    public class AccessLog
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string TargetId { get; set; } // QrCodeId or TemporaryQrCodeId or StaffId
        
        [Required]
        [MaxLength(20)]
        public string TargetType { get; set; } // "Student", "Visitor", "Staff"
        
        public DateTime Timestamp { get; set; } = DateTime.Now;
        
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } // "Granted", "Denied"

        [Required]
        [MaxLength(10)]
        public string Direction { get; set; } // "Entry", "Exit"
        
        [MaxLength(255)]
        public string Reason { get; set; } 
        
        public int? GuardId { get; set; }
        [ForeignKey("GuardId")]
        public User Guard { get; set; }
        
        [MaxLength(50)]
        public string GateLocation { get; set; }
    }
}
