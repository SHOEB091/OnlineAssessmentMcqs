using System.Text.Json.Serialization;
using OnlineAssessment.Web.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace OnlineAssessment.Web.Models
{
    public class Question
    {
        public int Id { get; set; }
        
        [Required]
        public required string Text { get; set; }
        
        [Required]
        public required string Title { get; set; }
        
        public QuestionType Type { get; set; }
        
        public int TestId { get; set; } // ✅ Foreign key (Required)

        [JsonIgnore] // ✅ Prevent circular reference in API response
        public Test? Test { get; set; } // ✅ Navigation property

        public ICollection<AnswerOption> AnswerOptions { get; set; } = new List<AnswerOption>(); // ✅ MCQ Options
        public ICollection<TestCase> TestCases { get; set; } = new List<TestCase>(); // ✅ Coding test cases

        // New properties for coding questions
        public string? FunctionName { get; set; }
        public string? ReturnType { get; set; }
        public string? ReturnDescription { get; set; }

        private string? _constraintsJson;
        [NotMapped]
        public List<string>? Constraints
        {
            get => _constraintsJson == null ? null : JsonSerializer.Deserialize<List<string>>(_constraintsJson);
            set => _constraintsJson = value == null ? null : JsonSerializer.Serialize(value);
        }

        private string? _starterCodeJson;
        [NotMapped]
        public Dictionary<string, string>? StarterCode
        {
            get => _starterCodeJson == null ? null : JsonSerializer.Deserialize<Dictionary<string, string>>(_starterCodeJson);
            set => _starterCodeJson = value == null ? null : JsonSerializer.Serialize(value);
        }

        private string? _parametersJson;
        [NotMapped]
        public List<Parameter>? Parameters
        {
            get => _parametersJson == null ? null : JsonSerializer.Deserialize<List<Parameter>>(_parametersJson);
            set => _parametersJson = value == null ? null : JsonSerializer.Serialize(value);
        }

        // Backing fields for JSON storage
        public string? ConstraintsJson
        {
            get => _constraintsJson;
            set => _constraintsJson = value;
        }

        public string? StarterCodeJson
        {
            get => _starterCodeJson;
            set => _starterCodeJson = value;
        }

        public string? ParametersJson
        {
            get => _parametersJson;
            set => _parametersJson = value;
        }
    }

    public class Parameter
    {
        public required string Name { get; set; }
        public required string Type { get; set; }
        public required string Description { get; set; }
    }
}
