using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OnlineAssessment.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace OnlineAssessment.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var tests = await _context.Tests
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
            return View(tests);
        }

        public async Task<IActionResult> Questions()
        {
            var questions = await _context.Questions
                .Include(q => q.AnswerOptions)
                .Include(q => q.TestCases)
                .ToListAsync();
            return View(questions);
        }

        public IActionResult UploadMcqQuestions()
        {
            return View();
        }

        public IActionResult UploadCodingQuestions()
        {
            return View();
        }
    }
} 