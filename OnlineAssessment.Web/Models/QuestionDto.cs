using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OnlineAssessment.Web.Models
{
    public class QuestionDto
    {
        [Required]
        public string Text { get; set; } = string.Empty;
        
        [Required]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public QuestionType Type { get; set; }
        
        public int TestId { get; set; }
        public List<AnswerOptionDto>? AnswerOptions { get; set; }
        public List<TestCaseDto>? TestCases { get; set; }

        // New properties for coding questions
        public string? FunctionName { get; set; }
        public string? ReturnType { get; set; }
        public string? ReturnDescription { get; set; }
        public List<string>? Constraints { get; set; }
        public Dictionary<string, string>? StarterCode { get; set; }
        public List<Parameter>? Parameters { get; set; }
    }
}
