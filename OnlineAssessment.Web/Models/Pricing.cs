using System.ComponentModel.DataAnnotations;

namespace OnlineAssessment.Web.Models
{
    public class Pricing
    {
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; }
        
        [Required]
        public decimal Price { get; set; }
        
        [Required]
        public int MaxStudents { get; set; }
        
        public bool IncludesMcq { get; set; }
        
        public bool IncludesCoding { get; set; }
        
        public string? Description { get; set; }
    }
} 