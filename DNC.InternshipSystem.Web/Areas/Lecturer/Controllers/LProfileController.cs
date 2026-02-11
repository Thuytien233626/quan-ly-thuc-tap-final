using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DNC.InternshipSystem.Core.Entities;
using DNC.InternshipSystem.Infrastructure.Data;

namespace DNC.InternshipSystem.Web.Areas.Lecturer.Controllers
{
    [Area("Lecturer")]
    [Authorize(Roles = "Lecturer")]
    public class LProfileController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public LProfileController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var lecturer = await _context.Lecturers
                .Include(l => l.User)
                .Include(l => l.Department)
                .Include(l => l.Classes)
                .FirstOrDefaultAsync(l => l.UserId == user.Id);

            // Thong ke nhanh
            int assignedStudents = 0;
            int totalLogbooks = 0;
            int gradedStudents = 0;

            if (lecturer != null)
            {
                assignedStudents = await _context.Registrations
                    .CountAsync(r => r.LecturerId == lecturer.UserId && r.Status == 1);

                totalLogbooks = await _context.Logbooks
                    .Include(l => l.Registration)
                    .CountAsync(l => l.Registration!.LecturerId == lecturer.UserId);

                gradedStudents = await _context.Grades
                    .Include(g => g.Registration)
                    .CountAsync(g => g.Registration!.LecturerId == lecturer.UserId && g.InstructorScore.HasValue);
            }

            ViewBag.Lecturer = lecturer;
            ViewBag.AssignedStudents = assignedStudents;
            ViewBag.TotalLogbooks = totalLogbooks;
            ViewBag.GradedStudents = gradedStudents;

            return View();
        }
    }
}
