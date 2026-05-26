namespace CampusSentinel.Models
{
    /// <summary>
    /// Lightweight read DTO returned after a QR code is resolved.
    /// Carries only the public-facing fields needed to populate the
    /// Admin's scanning dashboard — no sensitive auth data is included.
    /// </summary>
    public class ChallanSubjectDto
    {
        /// <summary>The original QR code value that was scanned / entered.</summary>
        public string QrCodeId { get; set; }

        /// <summary>Full name of the identified person.</summary>
        public string FullName { get; set; }

        /// <summary>Person category: "Student", "Staff", or "Visitor".</summary>
        public string SubjectType { get; set; }

        /// <summary>
        /// Additional contextual detail shown on the profile card
        /// (e.g. Registration No for students, Designation for staff).
        /// </summary>
        public string? Detail { get; set; }
    }
}
