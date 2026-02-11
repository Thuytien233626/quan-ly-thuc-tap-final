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
    public class RResultController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public RResultController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Student/RResult
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            // Tim registration cua sinh vien (moi nhat)
            var registration = await _context.Registrations
                .Include(r => r.Company)
                .Include(r => r.Lecturer)
                    .ThenInclude(l => l!.User)
                .Include(r => r.Term)
                    .ThenInclude(t => t!.Batch)
                .Where(r => r.StudentId == user.Id)
                .OrderByDescending(r => r.CreatedDate)
                .FirstOrDefaultAsync();

            // Tim diem (Grade)
            Grade? grade = null;
            if (registration != null)
            {
                grade = await _context.Grades
                    .FirstOrDefaultAsync(g => g.RegistrationId == registration.Id);
            }

            // Dem so logbook
            int logbookCount = 0;
            int totalWeeks = 0;
            if (registration != null)
            {
                logbookCount = await _context.Logbooks
                    .CountAsync(l => l.RegistrationId == registration.Id);
                totalWeeks = registration.Term?.DurationInWeeks ?? 0;
            }

            ViewBag.Registration = registration;
            ViewBag.Grade = grade;
            ViewBag.LogbookCount = logbookCount;
            ViewBag.TotalWeeks = totalWeeks;
            ViewBag.HasRegistration = registration != null;

            return View();
        }
    }
}
