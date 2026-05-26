using System;
using System.ComponentModel.DataAnnotations;

namespace CampusSentinel.Models
{
    public abstract class User
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Username { get; set; }
        
        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string Role { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
    }
}
