using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineAssessment.Web.Models
{
    public class TestCase
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public required string Input { get; set; }

        [Required]
        public required string ExpectedOutput { get; set; }

        public string? Explanation { get; set; }

        public int QuestionId { get; set; }

        [ForeignKey("QuestionId")]
        public Question Question { get; set; } = null!;
    }
}
