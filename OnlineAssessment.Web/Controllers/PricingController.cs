using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineAssessment.Web.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace OnlineAssessment.Web.Controllers
{
    public class PricingController : Controller
    {
        private readonly AppDbContext _context;

        public PricingController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var pricings = await _context.Pricings.ToListAsync();
            return View(pricings);
        }

        [HttpGet]
        [Authorize(Roles = "Organization")]
        public async Task<IActionResult> Subscribe(int pricingId)
        {
            var pricing = await _context.Pricings.FindAsync(pricingId);
            if (pricing == null)
            {
                return NotFound();
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
            {
                TempData["Error"] = "User ID not found. Please log in again.";
                return RedirectToAction("Login", "Auth");
            }

            if (!int.TryParse(userIdClaim.Value, out int userId))
            {
                TempData["Error"] = "Invalid user ID format.";
                return RedirectToAction("Login", "Auth");
            }

            var existingSubscription = await _context.OrganizationSubscriptions
                .FirstOrDefaultAsync(s => s.OrganizationId == userId && s.IsActive);

            if (existingSubscription != null)
            {
                TempData["Error"] = "You already have an active subscription.";
                return RedirectToAction(nameof(Index));
            }

            var subscription = new OrganizationSubscription
            {
                OrganizationId = userId,
                PricingId = pricingId,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(1),
                IsActive = true,
                CurrentStudentCount = 0
            };

            _context.OrganizationSubscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Successfully subscribed to the pricing plan!";
            return RedirectToAction("Index", "Test");
        }

        [HttpGet]
        [Authorize(Roles = "Organization")]
        public async Task<IActionResult> MySubscription()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
            {
                TempData["Error"] = "User ID not found. Please log in again.";
                return RedirectToAction("Login", "Auth");
            }

            if (!int.TryParse(userIdClaim.Value, out int userId))
            {
                TempData["Error"] = "Invalid user ID format.";
                return RedirectToAction("Login", "Auth");
            }

            var subscription = await _context.OrganizationSubscriptions
                .Include(s => s.Pricing)
                .FirstOrDefaultAsync(s => s.OrganizationId == userId && s.IsActive);

            if (subscription == null)
            {
                return RedirectToAction(nameof(Index));
            }

            return View(subscription);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Pricing pricing)
        {
            if (ModelState.IsValid)
            {
                _context.Pricings.Add(pricing);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(pricing);
        }
    }
} 