using System.ComponentModel.DataAnnotations;

namespace OnlineAssessment.Web.Models
{
    public class TestCaseDto
    {
        public required string Input { get; set; }
        public required string ExpectedOutput { get; set; }
        public string? Explanation { get; set; }
    }
}
