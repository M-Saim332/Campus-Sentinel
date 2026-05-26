using System;
using System.ComponentModel.DataAnnotations;

namespace CampusSentinel.Models
{
    public enum StaffCategory
    {
        Faculty,
        Helper,
        Gardener,
        Worker
    }

    public class Staff
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Formatted ID: 
        /// Faculty: FR-[ID]-[Dept]
        /// Helper: HP-[ID]-[Dept]
        /// Gardener: GD-[ID]-[Uni]
        /// </summary>
        [MaxLength(50)]
        [Display(Name = "Staff ID / QR Code")]
        public string StaffId { get; set; }

        [Required]
        public StaffCategory Category { get; set; }

        [Required]
        [MaxLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required]
        [MaxLength(50)]
        [Display(Name = "Designation")]
        public string Designation { get; set; }

        [Required]
        [MaxLength(50)]
        [Display(Name = "Department / Uni Name")]
        public string DepartmentOrUni { get; set; }

        [Required]
        [MaxLength(10)]
        public string Gender { get; set; } // Male, Female

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        public bool IsBlacklisted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
