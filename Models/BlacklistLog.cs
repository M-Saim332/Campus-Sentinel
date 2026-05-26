using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusSentinel.Models
{
    public class BlacklistLog
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string TargetId { get; set; } // QrCodeId or TemporaryQrCodeId
        
        [Required]
        [MaxLength(20)]
        public string TargetType { get; set; } // "Student" or "Visitor"
        
        [Required]
        [MaxLength(255)]
        public string Reason { get; set; }
        
        public DateTime BlacklistedAt { get; set; } = DateTime.Now;
        
        public int? BlacklistedBy { get; set; } // UserId
        [ForeignKey("BlacklistedBy")]
        public User BlacklistedByUser { get; set; }
    }
}
