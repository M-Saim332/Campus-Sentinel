using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusSentinel.Models
{
    public class Incident
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        public IncidentSeverity Severity { get; set; }

        public IncidentStatus Status { get; set; }

        [MaxLength(100)]
        public string Location { get; set; }

        // Use int to match the existing User entity
        public int ReportedById { get; set; }

        [ForeignKey("ReportedById")]
        public User ReportedBy { get; set; }

        public DateTime ReportedAt { get; set; } = DateTime.Now;

        public DateTime? ResolvedAt { get; set; }

        public int? LinkedPersonId { get; set; }

        public PersonType? LinkedPersonType { get; set; }

        public int? LinkedAccessLogId { get; set; }

        [ForeignKey("LinkedAccessLogId")]
        public AccessLog LinkedAccessLog { get; set; }

        public ICollection<IncidentNote> Notes { get; set; } = new List<IncidentNote>();
    }
}
