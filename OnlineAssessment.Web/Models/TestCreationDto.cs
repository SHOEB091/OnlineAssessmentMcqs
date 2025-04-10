using System.ComponentModel.DataAnnotations;

namespace OnlineAssessment.Web.Models
{
    public class TestCreationDto
    {
        [Required]
        public string Title { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        [Required]
        [Range(1, 1440)]
        public int DurationMinutes { get; set; }
        
        [Required]
        public TestType Type { get; set; }
        
        public List<QuestionDto> Questions { get; set; } = new();
    }
} 