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
    public class StudentHomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public StudentHomeController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            // Lay thong tin sinh vien
            var student = await _context.Students
                .Include(s => s.Class)
                    .ThenInclude(c => c!.Major)
                .Include(s => s.Class)
                    .ThenInclude(c => c!.Lecturer)
                        .ThenInclude(l => l.User)
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == user.Id);

            // Lay don dang ky moi nhat cua sinh vien (neu co)
            Registration? registration = null;
            if (student != null)
            {
                registration = await _context.Registrations
                    .Include(r => r.Company)
                    .Include(r => r.Lecturer)
                        .ThenInclude(l => l!.User)
                    .Include(r => r.Term)
                        .ThenInclude(t => t!.Batch)
                    .Where(r => r.StudentId == student.UserId)
                    .OrderByDescending(r => r.CreatedDate)
                    .FirstOrDefaultAsync();
            }

            // Lay dot thuc tap hien tai (isActive)
            var currentTerm = await _context.InternshipTerms
                .Include(t => t.Batch)
                .Where(t => t.IsActive)
                .OrderByDescending(t => t.Id)
                .FirstOrDefaultAsync();

            // Dem so logbook da nop
            int logbookCount = 0;
            if (registration != null)
            {
                logbookCount = await _context.Logbooks
                    .CountAsync(l => l.RegistrationId == registration.Id);
            }

            ViewBag.Student = student;
            ViewBag.Registration = registration;
            ViewBag.CurrentTerm = currentTerm;
            ViewBag.LogbookCount = logbookCount;
            ViewBag.UserFullName = user.FullName;

            return View();
        }
    }
}