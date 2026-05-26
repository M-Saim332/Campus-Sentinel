using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusSentinel.Models
{
    public class ShiftSwapRequest
    {
        [Key]
        public int Id { get; set; }

        public int RequestingGuardId { get; set; }

        [ForeignKey("RequestingGuardId")]
        public User RequestingGuard { get; set; }

        public int TargetGuardId { get; set; }

        [ForeignKey("TargetGuardId")]
        public User TargetGuard { get; set; }

        public int ShiftId { get; set; }

        [ForeignKey("ShiftId")]
        public GuardShift Shift { get; set; }

        [Required]
        [MaxLength(500)]
        public string Reason { get; set; }

        public SwapRequestStatus Status { get; set; } = SwapRequestStatus.Pending;

        public DateTime RequestedAt { get; set; } = DateTime.Now;

        public DateTime? ResolvedAt { get; set; }

        public int? ResolvedByAdminId { get; set; }

        [ForeignKey("ResolvedByAdminId")]
        public User ResolvedByAdmin { get; set; }
    }
}
