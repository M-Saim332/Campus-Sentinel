using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusSentinel.Models
{
    /// <summary>
    /// Represents a digital challan (fine / penalty) issued by an authorised campus
    /// official against a Student, Staff member, or Visitor.
    ///
    /// Relationship design:
    ///   • IssuedByUserId  → Users.Id  (FK — the official who raised the challan)
    ///   • QrCodeId        → string ref matching Student.QrCodeId | Staff.StaffId |
    ///                       Visitor.TemporaryQrCodeId.  A typed FK is intentionally
    ///                       avoided because subjects live in three separate tables;
    ///                       relational integrity is enforced at the service layer.
    ///   • SubjectName / SubjectType are denormalised at issuance time so the record
    ///     remains readable regardless of future changes to the source person record.
    /// </summary>
    public class Challan
    {
        /// <summary>Auto-generated primary key.</summary>
        [Key]
        public int Id { get; set; }

        // ── Subject identification ───────────────────────────────────────────

        /// <summary>
        /// The QR code value that was scanned at the time of issuance.
        /// Matches Student.QrCodeId, Staff.StaffId, or Visitor.TemporaryQrCodeId.
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Display(Name = "QR Code")]
        public string QrCodeId { get; set; }

        /// <summary>
        /// Full name of the subject, resolved from the source table at issuance.
        /// Denormalised so the challan remains self-contained.
        /// </summary>
        [Required]
        [MaxLength(100)]
        [Display(Name = "Subject Name")]
        public string SubjectName { get; set; }

        /// <summary>Person category: "Student", "Staff", or "Visitor".</summary>
        [Required]
        [MaxLength(20)]
        [Display(Name = "Subject Type")]
        public string SubjectType { get; set; }

        // ── Violation details ────────────────────────────────────────────────

        /// <summary>Short label describing the violation (e.g. "Smoking in prohibited zone").</summary>
        [Required]
        [MaxLength(100)]
        [Display(Name = "Violation Type")]
        public string ViolationType { get; set; }

        /// <summary>Optional extended description or evidence notes.</summary>
        [Display(Name = "Description")]
        public string? Description { get; set; }

        /// <summary>
        /// Monetary penalty in PKR.
        /// Must be greater than zero — enforced via DB CHECK constraint and service validation.
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Amount (PKR)")]
        public decimal Amount { get; set; }

        // ── Audit trail ──────────────────────────────────────────────────────

        /// <summary>FK to the Users table — the official who issued this challan.</summary>
        public int IssuedByUserId { get; set; }

        /// <summary>Navigation property to the issuing official.</summary>
        [ForeignKey("IssuedByUserId")]
        public User IssuedBy { get; set; }

        /// <summary>UTC timestamp at the moment of challan creation.</summary>
        [Display(Name = "Issue Date")]
        public DateTime IssueDate { get; set; } = DateTime.UtcNow;

        // ── Lifecycle ────────────────────────────────────────────────────────

        /// <summary>Current status of the challan (Pending → Paid / Disputed / Cancelled).</summary>
        [Display(Name = "Status")]
        public ChallanStatus Status { get; set; } = ChallanStatus.Pending;

        /// <summary>
        /// Optional administrative notes added during status transitions,
        /// e.g. a payment reference number or cancellation reason.
        /// </summary>
        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
