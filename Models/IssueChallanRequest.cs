using System.ComponentModel.DataAnnotations;

namespace CampusSentinel.Models
{
    /// <summary>
    /// Command DTO that carries the validated input required to issue a challan.
    /// Accepted by <c>IChallanService.IssueChallanAsync</c>.
    /// All server-side validation rules are defined here via Data Annotations
    /// so they are enforced at both the service boundary and the Razor Page layer.
    /// </summary>
    public class IssueChallanRequest
    {
        /// <summary>The QR code of the person being fined.</summary>
        [Required(ErrorMessage = "A QR code must be scanned before issuing a challan.")]
        [MaxLength(50)]
        public string QrCodeId { get; set; }

        /// <summary>Short label for the violation (e.g. "Smoking in prohibited area").</summary>
        [Required(ErrorMessage = "Violation type cannot be empty.")]
        [MaxLength(100, ErrorMessage = "Violation type cannot exceed 100 characters.")]
        [Display(Name = "Violation Type")]
        public string ViolationType { get; set; }

        /// <summary>Optional extended description or supporting notes.</summary>
        [MaxLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
        public string? Description { get; set; }

        /// <summary>
        /// Penalty amount in PKR. Must be strictly greater than zero.
        /// The [Range] attribute provides the UI/model-binding layer; the service
        /// performs an additional explicit check before persisting.
        /// </summary>
        [Required(ErrorMessage = "An amount is required.")]
        [Range(0.01, 1_000_000, ErrorMessage = "Amount must be greater than 0 and realistic.")]
        [Display(Name = "Amount (PKR)")]
        public decimal Amount { get; set; }

        /// <summary>The User.Id of the official issuing the challan (resolved from the auth cookie).</summary>
        public int IssuedByUserId { get; set; }
    }
}
