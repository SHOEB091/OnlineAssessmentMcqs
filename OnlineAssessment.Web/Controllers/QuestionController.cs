using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineAssessment.Web.Models;
using OnlineAssessment.Web.Models.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using System.Text.Json;

namespace OnlineAssessment.Web.Controllers
{
    [Route("api/questions")]
    [ApiController]
    public class QuestionController : Controller
    {
        private readonly AppDbContext _context;

        public QuestionController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("create")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateQuestion([FromBody] QuestionDto questionDto)
        {
            if (string.IsNullOrWhiteSpace(questionDto.Text))
                return BadRequest(new { message = "Question text is required" });

            if (questionDto.TestId <= 0)
                return BadRequest(new { message = "Test ID is required" });

            var question = new Question
            {
                Text = questionDto.Text,
                Type = questionDto.Type,
                TestId = questionDto.TestId,
                Title = questionDto.Text.Length > 100 ? questionDto.Text.Substring(0, 100) : questionDto.Text
            };

            if (questionDto.Type == QuestionType.MultipleChoice)
            {
                if (questionDto.AnswerOptions == null || questionDto.AnswerOptions.Count < 2)
                    return BadRequest(new { message = "Multiple choice questions require at least 2 answer options" });

                question.AnswerOptions = questionDto.AnswerOptions.Select(option => new AnswerOption
                {
                    Text = option.Text,
                    IsCorrect = option.IsCorrect
                }).ToList();
            }
            else if (questionDto.Type == QuestionType.ShortAnswer)
            {
                if (questionDto.TestCases == null || !questionDto.TestCases.Any())
                    return BadRequest(new { message = "Short answer questions require at least one test case" });

                question.TestCases = questionDto.TestCases.Select(testCase => new TestCase
                {
                    Input = testCase.Input,
                    ExpectedOutput = testCase.ExpectedOutput
                }).ToList();
            }
            else if (questionDto.Type == QuestionType.Coding)
            {
                if (string.IsNullOrWhiteSpace(questionDto.FunctionName))
                    return BadRequest(new { message = "Function name is required for coding questions" });

                if (string.IsNullOrWhiteSpace(questionDto.ReturnType))
                    return BadRequest(new { message = "Return type is required for coding questions" });

                if (questionDto.TestCases == null || !questionDto.TestCases.Any())
                    return BadRequest(new { message = "Coding questions require at least one test case" });

                question.FunctionName = questionDto.FunctionName;
                question.ReturnType = questionDto.ReturnType;
                question.ReturnDescription = questionDto.ReturnDescription;
                question.Constraints = questionDto.Constraints;
                question.StarterCode = questionDto.StarterCode;
                question.Parameters = questionDto.Parameters;
                question.TestCases = questionDto.TestCases.Select(testCase => new TestCase
                {
                    Input = testCase.Input,
                    ExpectedOutput = testCase.ExpectedOutput
                }).ToList();
            }

            _context.Questions.Add(question);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Question created successfully", question });
        }

        [HttpGet("test/{testId}")]
        [Authorize]
        public async Task<IActionResult> GetQuestionsByTest(int testId)
        {
            var questions = await _context.Questions
                .Where(q => q.TestId == testId)
                .Include(q => q.AnswerOptions)
                .Include(q => q.TestCases)
                .ToListAsync();

            return Ok(questions);
        }

        // ✅ Get a single question by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetQuestionById(int id)
        {
            var question = await _context.Questions
                .Include(q => q.AnswerOptions)
                .Include(q => q.TestCases)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null)
            {
                return NotFound(new { message = "Question not found." });
            }

            return Ok(question);
        }

        // ✅ Delete a question
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var question = await _context.Questions
                .Include(q => q.AnswerOptions)
                .Include(q => q.TestCases)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null)
            {
                return NotFound(new { message = "Question not found." });
            }

            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Question deleted successfully!" });
        }

        [HttpPost("upload-json")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UploadJsonQuestions([FromBody] List<QuestionDto> questions)
        {
            if (questions == null || !questions.Any())
            {
                return BadRequest(new { message = "No questions provided" });
            }

            try
            {
                foreach (var questionDto in questions)
                {
                    if (string.IsNullOrWhiteSpace(questionDto.Text))
                    {
                        return BadRequest(new { message = "Question text cannot be empty" });
                    }

                    // Check if test exists
                    var testExists = await _context.Tests.AnyAsync(t => t.Id == questionDto.TestId);
                    if (!testExists)
                    {
                        return BadRequest(new { message = $"Test with ID {questionDto.TestId} does not exist" });
                    }

                    var question = new Question
                    {
                        Text = questionDto.Text,
                        Type = questionDto.Type,
                        TestId = questionDto.TestId,
                        Title = questionDto.Text.Length > 100 ? questionDto.Text.Substring(0, 100) : questionDto.Text
                    };

                    if (questionDto.Type == QuestionType.MultipleChoice)
                    {
                        if (questionDto.AnswerOptions == null || questionDto.AnswerOptions.Count < 2)
                        {
                            return BadRequest(new { message = "Multiple choice questions require at least 2 answer options" });
                        }

                        question.AnswerOptions = questionDto.AnswerOptions.Select(option => new AnswerOption
                        {
                            Text = option.Text,
                            IsCorrect = option.IsCorrect
                        }).ToList();
                    }
                    else if (questionDto.Type == QuestionType.ShortAnswer)
                    {
                        if (questionDto.TestCases != null && questionDto.TestCases.Any())
                        {
                            question.TestCases = questionDto.TestCases.Select(testCase => new TestCase
                            {
                                Input = testCase.Input,
                                ExpectedOutput = testCase.ExpectedOutput
                            }).ToList();
                        }
                    }
                    else if (questionDto.Type == QuestionType.Coding)
                    {
                        if (string.IsNullOrWhiteSpace(questionDto.FunctionName))
                        {
                            return BadRequest(new { message = "Function name is required for coding questions" });
                        }

                        if (string.IsNullOrWhiteSpace(questionDto.ReturnType))
                        {
                            return BadRequest(new { message = "Return type is required for coding questions" });
                        }

                        if (questionDto.TestCases == null || !questionDto.TestCases.Any())
                        {
                            return BadRequest(new { message = "Coding questions require at least one test case" });
                        }

                        question.FunctionName = questionDto.FunctionName;
                        question.ReturnType = questionDto.ReturnType;
                        question.ReturnDescription = questionDto.ReturnDescription;
                        question.Constraints = questionDto.Constraints;
                        question.StarterCode = questionDto.StarterCode;
                        question.Parameters = questionDto.Parameters;
                        question.TestCases = questionDto.TestCases.Select(testCase => new TestCase
                        {
                            Input = testCase.Input,
                            ExpectedOutput = testCase.ExpectedOutput
                        }).ToList();
                    }

                    _context.Questions.Add(question);
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Questions uploaded and stored successfully", count = questions.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error storing questions", error = ex.Message });
            }
        }

        [HttpPost("upload-file")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UploadFile(IFormFile file, int testId)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { success = false, message = "No file uploaded" });
            }

            if (!file.FileName.EndsWith(".json"))
            {
                return BadRequest(new { success = false, message = "Only JSON files are allowed" });
            }

            try
            {
                using var stream = file.OpenReadStream();
                using var reader = new StreamReader(stream);
                var jsonContent = await reader.ReadToEndAsync();
                
                var questions = JsonSerializer.Deserialize<List<QuestionDto>>(jsonContent);
                
                if (questions == null || !questions.Any())
                {
                    return BadRequest(new { success = false, message = "No questions found in the file" });
                }

                // Update test ID for all questions
                foreach (var question in questions)
                {
                    question.TestId = testId;
                }

                // Check if test exists
                var testExists = await _context.Tests.AnyAsync(t => t.Id == testId);
                if (!testExists)
                {
                    return BadRequest(new { success = false, message = $"Test with ID {testId} does not exist" });
                }

                foreach (var questionDto in questions)
                {
                    if (string.IsNullOrWhiteSpace(questionDto.Text))
                    {
                        return BadRequest(new { success = false, message = "Question text cannot be empty" });
                    }

                    var question = new Question
                    {
                        Text = questionDto.Text,
                        Type = questionDto.Type,
                        TestId = questionDto.TestId,
                        Title = questionDto.Text.Length > 100 ? questionDto.Text.Substring(0, 100) : questionDto.Text
                    };

                    if (questionDto.Type == QuestionType.MultipleChoice)
                    {
                        if (questionDto.AnswerOptions == null || questionDto.AnswerOptions.Count < 2)
                        {
                            return BadRequest(new { success = false, message = "Multiple choice questions require at least 2 answer options" });
                        }

                        question.AnswerOptions = questionDto.AnswerOptions.Select(option => new AnswerOption
                        {
                            Text = option.Text,
                            IsCorrect = option.IsCorrect
                        }).ToList();
                    }
                    else if (questionDto.Type == QuestionType.ShortAnswer)
                    {
                        if (questionDto.TestCases != null && questionDto.TestCases.Any())
                        {
                            question.TestCases = questionDto.TestCases.Select(testCase => new TestCase
                            {
                                Input = testCase.Input,
                                ExpectedOutput = testCase.ExpectedOutput
                            }).ToList();
                        }
                    }
                    else if (questionDto.Type == QuestionType.Coding)
                    {
                        if (string.IsNullOrWhiteSpace(questionDto.FunctionName))
                        {
                            return BadRequest(new { success = false, message = "Function name is required for coding questions" });
                        }

                        if (string.IsNullOrWhiteSpace(questionDto.ReturnType))
                        {
                            return BadRequest(new { success = false, message = "Return type is required for coding questions" });
                        }

                        if (questionDto.TestCases == null || !questionDto.TestCases.Any())
                        {
                            return BadRequest(new { success = false, message = "Coding questions require at least one test case" });
                        }

                        question.FunctionName = questionDto.FunctionName;
                        question.ReturnType = questionDto.ReturnType;
                        question.ReturnDescription = questionDto.ReturnDescription;
                        question.Constraints = questionDto.Constraints;
                        question.StarterCode = questionDto.StarterCode;
                        question.Parameters = questionDto.Parameters;
                        question.TestCases = questionDto.TestCases.Select(testCase => new TestCase
                        {
                            Input = testCase.Input,
                            ExpectedOutput = testCase.ExpectedOutput
                        }).ToList();
                    }

                    _context.Questions.Add(question);
                }

                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Questions uploaded and stored successfully", count = questions.Count });
            }
            catch (JsonException ex)
            {
                return BadRequest(new { success = false, message = "Invalid JSON format", error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error processing file", error = ex.Message });
            }
        }
    }
}
