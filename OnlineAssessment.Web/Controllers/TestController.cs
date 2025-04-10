using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineAssessment.Web.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;

namespace OnlineAssessment.Web.Controllers
{
    public class TestController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<TestController> _logger;

        public TestController(AppDbContext context, IWebHostEnvironment environment, ILogger<TestController> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        // View action for test list page
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var isAdmin = User.IsInRole("Admin");
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var username = User.Identity?.Name ?? "Guest";

                ViewBag.IsAdmin = isAdmin;
                ViewBag.Username = username;

                if (userRole == "Organization" && userId != null)
                {
                    var organizationId = int.Parse(userId);
                    var hasActiveSubscription = await _context.OrganizationSubscriptions
                        .AnyAsync(s => s.OrganizationId == organizationId && s.IsActive);

                    ViewBag.HasActiveSubscription = hasActiveSubscription;

                    if (hasActiveSubscription)
                    {
                        var subscription = await _context.OrganizationSubscriptions
                            .Include(s => s.Pricing)
                            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId && s.IsActive);

                        if (subscription != null)
                        {
                            ViewBag.CanCreateMcq = subscription.Pricing.IncludesMcq;
                            ViewBag.CanCreateCoding = subscription.Pricing.IncludesCoding;
                        }
                    }

                    var tests = await _context.Tests
                        .Where(t => t.CreatedBy == organizationId)
                        .ToListAsync();

                    return View(tests);
                }
                else if (isAdmin)
                {
                    ViewBag.CanCreateMcq = true;
                    ViewBag.CanCreateCoding = true;
                    var tests = await _context.Tests.ToListAsync();
                    return View(tests);
                }

                return View(new List<Test>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Test Index action");
                return View(new List<Test>());
            }
        }

        // View action for uploading questions
        [HttpGet]
        [Route("Test/upload-questions/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UploadQuestions(int id)
        {
            try
            {
                var test = await _context.Tests.FindAsync(id);
                if (test == null)
                {
                    return NotFound();
                }

                return View("UploadQuestions", test);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error loading test: " + ex.Message });
            }
        }

        // View action for taking a test
        [HttpGet]
        [Route("Test/Take/{id}")]
        [Authorize]
        public async Task<IActionResult> Take(int id)
        {
            var test = await _context.Tests
                .Include(t => t.Questions)
                    .ThenInclude(q => q.AnswerOptions)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (test == null)
            {
                return NotFound();
            }

            return View(test);
        }

        [HttpGet]
        [Route("Test/view-uploads")]
        [Authorize(Roles = "Admin")]
        public IActionResult ViewUploads()
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var files = Directory.GetFiles(uploadsFolder)
                .Select(f => new FileInfo(f))
                .Select(f => new
                {
                    Name = f.Name,
                    Size = f.Length,
                    LastModified = f.LastWriteTime,
                    Path = $"/uploads/{f.Name}"
                })
                .ToList();

            return View(files);
        }

        [HttpPost]
        [Route("Test/upload-questions")]
        [Authorize(Roles = "Admin,Organization")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadQuestions([FromForm] IFormFile file, [FromForm] int testId)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return Json(new { success = false, message = "No file uploaded" });
                }

                if (!file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    return Json(new { success = false, message = "Only JSON files are allowed" });
                }

                if (file.Length > 10 * 1024 * 1024) // 10MB limit
                {
                    return Json(new { success = false, message = "File size exceeds 10MB limit" });
                }

                // Check if the test exists and belongs to the user
                var test = await _context.Tests.FindAsync(testId);
                if (test == null)
                {
                    return Json(new { success = false, message = "Test not found" });
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (userRole == "Organization" && userId != null)
                {
                    var organizationId = int.Parse(userId);
                    if (test.CreatedBy != organizationId)
                    {
                        return Json(new { success = false, message = "You can only upload questions to your own tests" });
                    }

                    // Check if the organization has an active subscription with MCQ access
                    var hasActiveSubscription = await _context.OrganizationSubscriptions
                        .Include(s => s.Pricing)
                        .AnyAsync(s => s.OrganizationId == organizationId && s.IsActive && s.Pricing.IncludesMcq);

                    if (!hasActiveSubscription)
                    {
                        return Json(new { success = false, message = "You need an active subscription with MCQ access to upload questions" });
                    }
                }

                using var reader = new StreamReader(file.OpenReadStream());
                var jsonContent = await reader.ReadToEndAsync();
                var questions = JsonSerializer.Deserialize<List<QuestionDto>>(jsonContent);

                if (questions == null || !questions.Any())
                {
                    return Json(new { success = false, message = "Invalid JSON format or empty questions" });
                }

                foreach (var questionDto in questions)
                {
                    if (string.IsNullOrWhiteSpace(questionDto.Text))
                    {
                        return Json(new { success = false, message = "Question text cannot be empty" });
                    }

                    var question = new Question
                    {
                        Text = questionDto.Text,
                        Type = QuestionType.MultipleChoice,
                        TestId = testId,
                        Title = questionDto.Text.Length > 100 ? questionDto.Text.Substring(0, 100) : questionDto.Text
                    };

                    _context.Questions.Add(question);
                    await _context.SaveChangesAsync();

                    if (questionDto.AnswerOptions != null)
                    {
                        foreach (var optionDto in questionDto.AnswerOptions)
                        {
                            _context.AnswerOptions.Add(new AnswerOption
                            {
                                Text = optionDto.Text,
                                IsCorrect = optionDto.IsCorrect,
                                QuestionId = question.Id
                            });
                        }
                    }
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Questions uploaded successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading questions");
                return Json(new { success = false, message = "Error uploading questions: " + ex.Message });
            }
        }

        [HttpDelete]
        [Route("Test/delete/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteTest(int id)
        {
            try
            {
                // Check if user is admin
                if (!User.IsInRole("Admin"))
                {
                    return Json(new { success = false, message = "Unauthorized: Admin access required." });
                }

                // Get test with all related data
                var test = await _context.Tests
                    .Include(t => t.Questions)
                        .ThenInclude(q => q.AnswerOptions)
                    .Include(t => t.Questions)
                        .ThenInclude(q => q.TestCases)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (test == null)
                {
                    return Json(new { success = false, message = "Test not found." });
                }

                // Begin transaction
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Delete all related data
                    foreach (var question in test.Questions)
                    {
                        // Delete answer options
                        if (question.AnswerOptions != null)
                        {
                            _context.AnswerOptions.RemoveRange(question.AnswerOptions);
                        }
                        
                        // Delete test cases
                        if (question.TestCases != null)
                        {
                            _context.TestCases.RemoveRange(question.TestCases);
                        }
                    }

                    // Delete questions
                    _context.Questions.RemoveRange(test.Questions);
                    
                    // Delete test
                    _context.Tests.Remove(test);

                    // Save changes
                    await _context.SaveChangesAsync();
                    
                    // Commit transaction
                    await transaction.CommitAsync();
                    
                    return Json(new { success = true, message = "Test deleted successfully!" });
                }
                catch (Exception ex)
                {
                    // Rollback transaction on error
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, $"Error deleting test {id} and its related data");
                    return Json(new { success = false, message = "An error occurred while deleting the test and its related data." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in DeleteTest action for test {id}");
                return Json(new { success = false, message = "An unexpected error occurred while deleting the test." });
            }
        }

        [HttpPost]
        [Route("Test/Submit/{id}")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int id, [FromBody] Dictionary<string, string> answers)
        {
            try
            {
                _logger.LogInformation($"Received test submission for test ID: {id} from user: {User.Identity?.Name}");
                _logger.LogInformation($"Received {answers?.Count ?? 0} answers");
                
                if (answers == null)
                {
                    _logger.LogWarning($"No answers provided for test ID: {id}");
                    return BadRequest(new { success = false, message = "No answers provided" });
                }

                var test = await _context.Tests
                    .Include(t => t.Questions)
                        .ThenInclude(q => q.AnswerOptions)
                    .Include(t => t.Questions)
                        .ThenInclude(q => q.TestCases)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (test == null)
                {
                    _logger.LogWarning($"Test not found with ID: {id}");
                    return NotFound(new { success = false, message = "Test not found" });
                }

                // Check if user has already submitted this test
                var username = User.Identity?.Name ?? "Anonymous";
                var existingSubmission = await _context.TestResults
                    .FirstOrDefaultAsync(r => r.TestId == id && r.Username == username);
                
                if (existingSubmission != null)
                {
                    _logger.LogWarning($"User {username} attempted to submit test {id} multiple times");
                    return BadRequest(new { success = false, message = "You have already submitted this test" });
                }

                int mcqCorrect = 0;
                int totalMcq = test.Questions.Count(q => q.Type == QuestionType.MultipleChoice);
                int codingCorrect = 0;
                int totalCoding = test.Questions.Count(q => q.Type == QuestionType.ShortAnswer);
                var evaluationDetails = new List<string>();

                // Evaluate MCQ questions
                foreach (var question in test.Questions.Where(q => q.Type == QuestionType.MultipleChoice))
                {
                    var questionNumber = test.Questions.ToList().IndexOf(question) + 1;
                    var selectedOptionId = answers.GetValueOrDefault($"question_{question.Id}");
                    
                    if (selectedOptionId != null)
                    {
                        var selectedOption = question.AnswerOptions.FirstOrDefault(a => a.Id.ToString() == selectedOptionId);
                        var isCorrect = selectedOption != null && selectedOption.IsCorrect;
                        if (isCorrect)
                        {
                            mcqCorrect++;
                        }
                        evaluationDetails.Add($"MCQ {questionNumber}: Selected: {selectedOption?.Text ?? "None"} - Correct: {isCorrect}");
                    }
                    else
                    {
                        evaluationDetails.Add($"MCQ {questionNumber}: No answer selected");
                    }
                }

                // Evaluate coding questions
                foreach (var question in test.Questions.Where(q => q.Type == QuestionType.ShortAnswer))
                {
                    var questionNumber = test.Questions.ToList().IndexOf(question) + 1;
                    var answer = answers.GetValueOrDefault($"question_{question.Id}");
                    
                    if (answer != null)
                    {
                        var testCase = question.TestCases.FirstOrDefault();
                        var isCorrect = testCase != null && answer.Trim().Equals(testCase.ExpectedOutput.Trim(), StringComparison.OrdinalIgnoreCase);
                        if (isCorrect)
                        {
                            codingCorrect++;
                        }
                        evaluationDetails.Add($"Coding {questionNumber}: Answer submitted - Passed test case: {isCorrect}");
                    }
                    else
                    {
                        evaluationDetails.Add($"Coding {questionNumber}: No answer provided");
                    }
                }

                // Calculate total score
                double mcqScore = totalMcq > 0 ? (double)mcqCorrect / totalMcq * 100 : 0; // MCQ worth 100%
                double totalScore = mcqScore; // Total score is now just MCQ score

                var result = new TestResult
                {
                    TestId = id,
                    Username = username,
                    TotalQuestions = totalMcq,
                    CorrectAnswers = mcqCorrect,
                    Score = totalScore,
                    McqScore = mcqScore,
                    CodingScore = 0, // Set coding score to 0 since we're not using it
                    SubmittedAt = DateTime.UtcNow
                };

                _context.TestResults.Add(result);
                await _context.SaveChangesAsync();

                return Ok(new { 
                    success = true, 
                    redirectUrl = $"/Test/Result/{result.Id}",
                    evaluationDetails = evaluationDetails,
                    score = totalScore,
                    mcqScore = mcqScore,
                    codingScore = 0,
                    mcqCorrect = mcqCorrect,
                    totalMcq = totalMcq,
                    codingCorrect = codingCorrect,
                    totalCoding = totalCoding
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting test {TestId}", id);
                return StatusCode(500, new { success = false, message = "Error submitting test: " + ex.Message });
            }
        }

        [HttpGet]
        [Route("Test/Result/{id}")]
        [Authorize]
        public async Task<IActionResult> Result(int id)
        {
            var result = await _context.TestResults
                .Include(r => r.Test)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (result == null)
            {
                return NotFound();
            }

            return View(result);
        }

        [HttpGet]
        [Route("Test/upload-coding-questions/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UploadCodingQuestions(int id)
        {
            try
            {
                var test = await _context.Tests.FindAsync(id);
                if (test == null)
                {
                    return NotFound();
                }

                return View("UploadCodingQuestions", test);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error loading test: " + ex.Message });
            }
        }

        [HttpPost]
        [Route("Test/upload-coding-questions")]
        [Authorize(Roles = "Admin,Organization")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadCodingQuestions([FromForm] IFormFile file, [FromForm] int testId)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return Json(new { success = false, message = "No file uploaded" });
                }

                if (!file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    return Json(new { success = false, message = "Only JSON files are allowed" });
                }

                if (file.Length > 10 * 1024 * 1024) // 10MB limit
                {
                    return Json(new { success = false, message = "File size exceeds 10MB limit" });
                }

                // Check if the test exists and belongs to the user
                var test = await _context.Tests.FindAsync(testId);
                if (test == null)
                {
                    return Json(new { success = false, message = "Test not found" });
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (userRole == "Organization" && userId != null)
                {
                    var organizationId = int.Parse(userId);
                    if (test.CreatedBy != organizationId)
                    {
                        return Json(new { success = false, message = "You can only upload questions to your own tests" });
                    }

                    // Check if the organization has an active subscription with Coding access
                    var hasActiveSubscription = await _context.OrganizationSubscriptions
                        .Include(s => s.Pricing)
                        .AnyAsync(s => s.OrganizationId == organizationId && s.IsActive && s.Pricing.IncludesCoding);

                    if (!hasActiveSubscription)
                    {
                        return Json(new { success = false, message = "You need an active subscription with Coding access to upload questions" });
                    }
                }

                using var reader = new StreamReader(file.OpenReadStream());
                var jsonContent = await reader.ReadToEndAsync();
                
                _logger.LogInformation($"Received JSON content for coding questions: {jsonContent.Substring(0, Math.Min(500, jsonContent.Length))}...");
                
                // Try to deserialize as a list of QuestionDto first
                List<QuestionDto> questions;
                try
                {
                    questions = JsonSerializer.Deserialize<List<QuestionDto>>(jsonContent);
                    _logger.LogInformation("Successfully deserialized JSON as List<QuestionDto>");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize as List<QuestionDto>, trying CodingQuestionsWrapper");
                    // If that fails, try to deserialize as an object with a codingQuestions property
                    try
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };
                        
                        var codingQuestionsWrapper = JsonSerializer.Deserialize<CodingQuestionsWrapper>(jsonContent, options);
                        questions = codingQuestionsWrapper?.CodingQuestions ?? new List<QuestionDto>();
                        
                        if (questions.Any())
                        {
                            _logger.LogInformation($"Successfully deserialized JSON as CodingQuestionsWrapper with {questions.Count} questions");
                        }
                    }
                    catch (Exception wrapperEx)
                    {
                        _logger.LogError(wrapperEx, "Failed to deserialize as CodingQuestionsWrapper");
                        return Json(new { success = false, message = "Invalid JSON format: " + wrapperEx.Message });
                    }
                }

                if (questions == null || !questions.Any())
                {
                    _logger.LogError("Questions list is null or empty after deserialization");
                    return Json(new { success = false, message = "Invalid JSON format or empty questions" });
                }

                _logger.LogInformation($"Processing {questions.Count} questions");
                
                foreach (var questionDto in questions)
                {
                    _logger.LogInformation($"Processing question: Text='{questionDto.Text}', Type={questionDto.Type}, FunctionName='{questionDto.FunctionName}'");
                    
                    if (string.IsNullOrWhiteSpace(questionDto.Text))
                    {
                        _logger.LogError("Question text is empty");
                        return Json(new { success = false, message = "Question text cannot be empty" });
                    }

                    if (string.IsNullOrWhiteSpace(questionDto.FunctionName))
                    {
                        _logger.LogError("Function name is empty");
                        return Json(new { success = false, message = "Function name is required for coding questions" });
                    }

                    if (string.IsNullOrWhiteSpace(questionDto.ReturnType))
                    {
                        _logger.LogError("Return type is empty");
                        return Json(new { success = false, message = "Return type is required for coding questions" });
                    }

                    if (questionDto.TestCases == null || !questionDto.TestCases.Any())
                    {
                        _logger.LogError("Test cases are missing or empty");
                        return Json(new { success = false, message = "At least one test case is required for coding questions" });
                    }

                    // Validate test cases
                    foreach (var testCase in questionDto.TestCases)
                    {
                        if (string.IsNullOrWhiteSpace(testCase.Input))
                        {
                            _logger.LogError("Test case input is empty");
                            return Json(new { success = false, message = "Test case input cannot be empty" });
                        }
                        if (string.IsNullOrWhiteSpace(testCase.ExpectedOutput))
                        {
                            _logger.LogError("Test case expected output is empty");
                            return Json(new { success = false, message = "Test case expected output cannot be empty" });
                        }
                    }

                    var question = new Question
                    {
                        Text = questionDto.Text,
                        Type = QuestionType.ShortAnswer,
                        TestId = testId,
                        Title = questionDto.Title,
                        FunctionName = questionDto.FunctionName,
                        ReturnType = questionDto.ReturnType,
                        ReturnDescription = questionDto.ReturnDescription,
                        Constraints = questionDto.Constraints,
                        StarterCode = questionDto.StarterCode,
                        Parameters = questionDto.Parameters,
                        TestCases = questionDto.TestCases.Select(tc => new TestCase
                        {
                            Input = tc.Input,
                            ExpectedOutput = tc.ExpectedOutput,
                            Explanation = tc.Explanation
                        }).ToList()
                    };

                    _context.Questions.Add(question);
                    await _context.SaveChangesAsync();
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Coding questions uploaded successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading coding questions");
                return Json(new { success = false, message = "Error uploading coding questions: " + ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Organization")]
        public async Task<IActionResult> Create([FromBody] Test test)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Invalid test data" });
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (userRole == "Organization" && userId != null)
                {
                    var organizationId = int.Parse(userId);
                    var hasActiveSubscription = await _context.OrganizationSubscriptions
                        .Include(s => s.Pricing)
                        .AnyAsync(s => s.OrganizationId == organizationId && s.IsActive);

                    if (!hasActiveSubscription)
                    {
                        return Json(new { success = false, message = "You need an active subscription to create tests" });
                    }

                    var subscription = await _context.OrganizationSubscriptions
                        .Include(s => s.Pricing)
                        .FirstOrDefaultAsync(s => s.OrganizationId == organizationId && s.IsActive);

                    if (subscription != null)
                    {
                        if (test.Type == TestType.MultipleChoice && !subscription.Pricing.IncludesMcq)
                        {
                            return Json(new { success = false, message = "Your subscription does not include MCQ tests" });
                        }

                        if (test.Type == TestType.Coding && !subscription.Pricing.IncludesCoding)
                        {
                            return Json(new { success = false, message = "Your subscription does not include Coding tests" });
                        }
                    }

                    test.CreatedBy = organizationId;
                }
                else if (!User.IsInRole("Admin"))
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }

                _context.Tests.Add(test);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Test created successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating test");
                return Json(new { success = false, message = "An error occurred while creating the test" });
            }
        }

        [HttpGet]
        [Route("Test/Share/{id}")]
        [Authorize(Roles = "Admin,Organization")]
        public async Task<IActionResult> Share(int id)
        {
            try
            {
                var test = await _context.Tests.FindAsync(id);
                if (test == null)
                {
                    return NotFound();
                }

                // Verify the user has permission to share this test
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (test.CreatedBy.ToString() != userId && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }

                ViewBag.ShareUrl = $"{Request.Scheme}://{Request.Host}/Test/Access/{test.ShareId}";
                return View(test);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Share action");
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        [Route("Test/Access/{shareId}")]
        public async Task<IActionResult> Access(string shareId)
        {
            try
            {
                var test = await _context.Tests
                    .Include(t => t.Questions)
                    .FirstOrDefaultAsync(t => t.ShareId == shareId);
                    
                if (test == null)
                {
                    return NotFound();
                }

                // If user is not logged in, show the test ID entry page
                if (User.Identity == null || !User.Identity.IsAuthenticated)
                {
                    ViewBag.ShareId = shareId;
                    return View("EnterTestId");
                }

                // Show test details page before starting the test
                return View("TestDetails", test);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Access action");
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [Route("Test/StartShared/{shareId}")]
        public async Task<IActionResult> StartShared(string shareId)
        {
            try
            {
                var test = await _context.Tests.FirstOrDefaultAsync(t => t.ShareId == shareId);
                if (test == null)
                {
                    return NotFound();
                }

                // Redirect to take the test
                return RedirectToAction(nameof(Take), new { id = test.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in StartShared action");
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        [Route("Test/Details/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var test = await _context.Tests
                    .Include(t => t.Questions)
                        .ThenInclude(q => q.AnswerOptions)
                    .Include(t => t.Questions)
                        .ThenInclude(q => q.TestCases)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (test == null)
                {
                    return NotFound();
                }

                return View(test);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Test Details action");
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        [Route("Test/Create")]
        [Authorize(Roles = "Admin,Organization")]
        public IActionResult Create()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (userRole == "Organization" && userId != null)
                {
                    var organizationId = int.Parse(userId);
                    var hasActiveSubscription = _context.OrganizationSubscriptions
                        .Any(s => s.OrganizationId == organizationId && s.IsActive);

                    if (!hasActiveSubscription)
                    {
                        return RedirectToAction(nameof(Index));
                    }

                    var subscription = _context.OrganizationSubscriptions
                        .Include(s => s.Pricing)
                        .FirstOrDefault(s => s.OrganizationId == organizationId && s.IsActive);

                    if (subscription != null)
                    {
                        ViewBag.CanCreateMcq = subscription.Pricing.IncludesMcq;
                        ViewBag.CanCreateCoding = subscription.Pricing.IncludesCoding;
                    }
                }
                else if (User.IsInRole("Admin"))
                {
                    ViewBag.CanCreateMcq = true;
                    ViewBag.CanCreateCoding = true;
                }

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Create action");
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        [Route("Test/History")]
        [Authorize]
        public async Task<IActionResult> History()
        {
            try
            {
                var username = User.Identity?.Name;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(username))
                {
                    return RedirectToAction("Login", "Auth");
                }

                // Only allow candidates to access test history
                if (userRole == "Admin" || userRole == "Organization")
                {
                    return RedirectToAction("Index");
                }

                // Candidates can only see their own test results
                var testResults = await _context.TestResults
                    .Include(r => r.Test)
                    .Where(r => r.Username == username)
                    .OrderByDescending(r => r.SubmittedAt)
                    .ToListAsync();

                return View(testResults);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving test history for user {Username}", User.Identity?.Name);
                return View(new List<TestResult>());
            }
        }

        [HttpGet]
        [Route("Test/OrganizationResults")]
        [Authorize(Roles = "Organization")]
        public async Task<IActionResult> OrganizationResults()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                {
                    return RedirectToAction("Login", "Auth");
                }

                var organizationId = int.Parse(userId);
                
                // Get all tests created by this organization
                var organizationTests = await _context.Tests
                    .Where(t => t.CreatedBy == organizationId)
                    .Select(t => t.Id)
                    .ToListAsync();

                // Get all results for these tests
                var testResults = await _context.TestResults
                    .Include(r => r.Test)
                    .Where(r => organizationTests.Contains(r.TestId))
                    .OrderByDescending(r => r.SubmittedAt)
                    .ToListAsync();

                return View(testResults);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving organization test results");
                return View(new List<TestResult>());
            }
        }
    }

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TestApiController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TestController> _logger;

        public TestApiController(AppDbContext context, ILogger<TestController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Create a new test
        [HttpPost("create")]
        [Authorize(Roles = "Admin,Organization")]
        public async Task<IActionResult> CreateTest([FromBody] Models.TestCreationDto testDto)
        {
            try
            {
                _logger.LogInformation($"Received test creation request: Title={testDto.Title}, Type={testDto.Type}, Duration={testDto.DurationMinutes}");
                
                if (string.IsNullOrWhiteSpace(testDto.Title))
                    return BadRequest(new { message = "Test title is required" });

                if (testDto.DurationMinutes <= 0 || testDto.DurationMinutes > 1440)
                    return BadRequest(new { message = "Duration must be between 1 and 1440 minutes" });

                // Get the current user's ID and role
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                
                // Check if organization has active subscription
                if (userRole == "Organization" && userId != null)
                {
                    var organizationId = int.Parse(userId);
                    var hasActiveSubscription = await _context.OrganizationSubscriptions
                        .AnyAsync(s => s.OrganizationId == organizationId && s.IsActive);
                        
                    if (!hasActiveSubscription)
                    {
                        return BadRequest(new { message = "You need an active subscription to create tests" });
                    }
                    
                    // Check if the organization has permission for the selected test type
                    var subscription = await _context.OrganizationSubscriptions
                        .Include(s => s.Pricing)
                        .FirstOrDefaultAsync(s => s.OrganizationId == organizationId && s.IsActive);
                        
                    if (subscription != null)
                    {
                        if (testDto.Type == TestType.MultipleChoice && !subscription.Pricing.IncludesMcq)
                        {
                            return BadRequest(new { message = "Your subscription does not include Multiple Choice tests" });
                        }
                        
                        if (testDto.Type == TestType.Coding && !subscription.Pricing.IncludesCoding)
                        {
                            return BadRequest(new { message = "Your subscription does not include Coding tests" });
                        }
                    }
                }

                // Create the test
                var test = new Test
                {
                    Title = testDto.Title,
                    Description = testDto.Description ?? $"Test created on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
                    DurationMinutes = testDto.DurationMinutes,
                    Type = testDto.Type,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userRole == "Organization" && userId != null ? int.Parse(userId) : null
                };

                _context.Tests.Add(test);
                await _context.SaveChangesAsync();

                // Add questions if provided
                if (testDto.Questions != null && testDto.Questions.Any())
                {
                    foreach (var questionDto in testDto.Questions)
                    {
                        if (string.IsNullOrWhiteSpace(questionDto.Text))
                            continue;

                        var question = new Question
                        {
                            Text = questionDto.Text,
                            Type = questionDto.Type,
                            TestId = test.Id,
                            Title = questionDto.Text.Length > 100 ? questionDto.Text.Substring(0, 100) : questionDto.Text
                        };

                        _context.Questions.Add(question);
                        await _context.SaveChangesAsync();

                        if (questionDto.Type == QuestionType.MultipleChoice && questionDto.AnswerOptions != null)
                        {
                            foreach (var optionDto in questionDto.AnswerOptions)
                            {
                                _context.AnswerOptions.Add(new AnswerOption
                                {
                                    Text = optionDto.Text,
                                    IsCorrect = optionDto.IsCorrect,
                                    QuestionId = question.Id
                                });
                            }
                        }
                        else if (questionDto.Type == QuestionType.ShortAnswer && questionDto.TestCases != null)
                        {
                            foreach (var testCaseDto in questionDto.TestCases)
                            {
                                _context.TestCases.Add(new TestCase
                                {
                                    Input = testCaseDto.Input,
                                    ExpectedOutput = testCaseDto.ExpectedOutput,
                                    Explanation = testCaseDto.Explanation,
                                    QuestionId = question.Id
                                });
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                }
                
                return Ok(new { 
                    message = "Test created successfully", 
                    test,
                    redirectUrl = $"/Test/Index"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error creating test: " + ex.Message });
            }
        }

        // Retrieve all tests
        [HttpGet("all")]
        public async Task<IActionResult> GetAllTests()
        {
            var tests = await _context.Tests.ToListAsync();
            return Ok(tests);
        }
    }
}

public class CodingQuestionsWrapper
{
    public List<QuestionDto> CodingQuestions { get; set; } = new();
}
