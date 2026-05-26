using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusSentinel.Models
{
    public class IncidentNote
    {
        [Key]
        public int Id { get; set; }

        public int IncidentId { get; set; }

        [ForeignKey("IncidentId")]
        public Incident Incident { get; set; }

        [Required]
        public string Note { get; set; }

        // Match existing User entity Id type
        public int AddedById { get; set; }

        [ForeignKey("AddedById")]
        public User AddedBy { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.Now;
    }
}
