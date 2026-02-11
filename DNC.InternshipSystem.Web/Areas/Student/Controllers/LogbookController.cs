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
    public class LogbookController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public LogbookController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Student/Logbook
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            // Tim registration cua sinh vien (moi nhat, da duyet)
            var registration = await _context.Registrations
                .Include(r => r.Term)
                .Where(r => r.StudentId == user.Id)
                .OrderByDescending(r => r.CreatedDate)
                .FirstOrDefaultAsync();

            if (registration != null)
            {
                // Lay danh sach logbook
                var logbooks = await _context.Logbooks
                    .Where(l => l.RegistrationId == registration.Id)
                    .OrderBy(l => l.WeekNumber)
                    .ToListAsync();

                int totalWeeks = registration.Term?.DurationInWeeks ?? 0;
                int submittedWeeks = logbooks.Count;

                ViewBag.Registration = registration;
                ViewBag.Logbooks = logbooks;
                ViewBag.TotalWeeks = totalWeeks;
                ViewBag.SubmittedWeeks = submittedWeeks;
                ViewBag.HasRegistration = true;
            }
            else
            {
                ViewBag.HasRegistration = false;
                ViewBag.Logbooks = new List<Logbook>();
                ViewBag.TotalWeeks = 0;
                ViewBag.SubmittedWeeks = 0;
            }

            return View();
        }
    }
}
