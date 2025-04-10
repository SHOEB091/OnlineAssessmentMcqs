using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineAssessment.Web.Models
{
    public class Test
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        [Range(1, 1440, ErrorMessage = "Duration must be between 1 and 1440 minutes.")]
        public int DurationMinutes { get; set; }

        [Required]
        public TestType Type { get; set; }

        [Required]
        [Column(TypeName = "datetime(6)")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;  // ✅ Use UtcNow to avoid timezone issues

        public int? CreatedBy { get; set; }  // ID of the organization that created the test

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Maximum number of students must be at least 1.")]
        public int MaxStudents { get; set; } = 100;  // Default value of 100 students

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Maximum attempts per student must be at least 1.")]
        public int MaxAttempts { get; set; } = 1;  // Default value of 1 attempt per student

        // New property for sharing
        public string ShareId { get; set; } = Guid.NewGuid().ToString("N");

        // ✅ Navigation property for related questions
        public ICollection<Question> Questions { get; set; } = new HashSet<Question>();
    }
}
