using System;
using System.ComponentModel.DataAnnotations;

namespace CampusSentinel.Models
{
    public class Visitor
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(50)]
        [Display(Name = "Temporary QR Code")]
        public string TemporaryQrCodeId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; }
        
        [MaxLength(255)]
        public string? Purpose { get; set; }

        [Required]
        [MaxLength(50)]
        public string Role { get; set; } = "Guest";
        
        public DateTime ExpirationTime { get; set; }
        
        public bool IsBlacklisted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
