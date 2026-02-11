using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DNC.InternshipSystem.Core.Entities;
using DNC.InternshipSystem.Infrastructure.Data;

namespace DNC.InternshipSystem.Web.Areas.Student.Controllers
{
    [Area("Student")]
    [Authorize(Roles = "Student")]
    public class SubmissionController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public SubmissionController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Student/Submission
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            // Tim registration cua sinh vien
            var registration = await _context.Registrations
                .Include(r => r.Term)
                .Where(r => r.StudentId == user.Id)
                .OrderByDescending(r => r.CreatedDate)
                .FirstOrDefaultAsync();

            // Dem logbook da nop
            int logbookCount = 0;
            int totalWeeks = 0;
            if (registration != null)
            {
                logbookCount = await _context.Logbooks
                    .CountAsync(l => l.RegistrationId == registration.Id);
                totalWeeks = registration.Term?.DurationInWeeks ?? 0;
            }

            ViewBag.Registration = registration;
            ViewBag.HasRegistration = registration != null;
            ViewBag.LogbookCount = logbookCount;
            ViewBag.TotalWeeks = totalWeeks;

            return View();
        }
    }
}
