using System.ComponentModel.DataAnnotations;

namespace OnlineAssessment.Web.Models
{
    public class Organization
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public required string Name { get; set; }

        [Required]
        public required string Description { get; set; }
    }
}
