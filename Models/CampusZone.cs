using System.ComponentModel.DataAnnotations;

namespace CampusSentinel.Models
{
    public class CampusZone
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(250)]
        public string Description { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
