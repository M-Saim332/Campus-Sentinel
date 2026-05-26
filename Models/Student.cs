using System;
using System.ComponentModel.DataAnnotations;

namespace CampusSentinel.Models
{
    public class Student
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Auto-generated barcode in format: Session-Dept-RegNo e.g. 2025-DS-129</summary>
        [MaxLength(50)]
        [Display(Name = "Barcode / QR Code ID")]
        public string QrCodeId { get; set; }

        [Required]
        [Display(Name = "Session (Year)")]
        public int Session { get; set; } = DateTime.Now.Year;

        [Required]
        [MaxLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required]
        [MaxLength(20)]
        [Display(Name = "Department Code")]
        public string Department { get; set; }

        [Required]
        [MaxLength(50)]
        [Display(Name = "Registration No.")]
        public string RegistrationNo { get; set; }

        [Required]
        [MaxLength(10)]
        public string Gender { get; set; } // Male, Female, Other

        [Required]
        [MaxLength(20)]
        [Display(Name = "Residency Type")]
        public string ResidencyType { get; set; } // Hostelite, Day Scholar

        public bool IsBlacklisted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
