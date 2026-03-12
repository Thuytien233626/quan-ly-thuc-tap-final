using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DNC.InternshipSystem.Core.Entities;
using DNC.InternshipSystem.Core.Enums;
using DNC.InternshipSystem.Infrastructure.Data;

namespace DNC.InternshipSystem.Web.Areas.Lecturer.Controllers
{
    [Area("Lecturer")]
    [Authorize(Roles = "Lecturer")]
    public class LecturerHomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public LecturerHomeController(AppDbContext context, UserManager<AppUser> userManager)
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
                .FirstOrDefaultAsync(l => l.UserId == user.Id);

            if (lecturer == null)
            {
                ViewBag.Message = "Hồ sơ giảng viên chưa được tạo.";
                return View();
            }

            // 1. Lay danh sach lop GV phu trach
            var myClasses = await _context.Classes
                .Where(c => c.LecturerId == lecturer.UserId && c.IsActive)
                .Include(c => c.Students)
                .Include(c => c.Major)
                .ToListAsync();

            var classIds = myClasses.Select(c => c.Id).ToList();

            // 2. Tong so sinh vien trong cac lop phu trach
            var totalStudents = myClasses.Sum(c => c.Students?.Count ?? 0);

            // 3. So sinh vien da dang ky thuc tap (co Registration)
            var registeredStudentsCount = await _context.Registrations
                .Include(r => r.Student)
                .CountAsync(r => r.Student != null && classIds.Contains(r.Student.ClassId!));

            // 4. So luong logbook cho duyet
            var pendingLogbooksCount = await _context.Logbooks
                .Include(l => l.Registration).ThenInclude(r => r!.Student)
                .CountAsync(l => string.IsNullOrEmpty(l.LecturerComment) &&
                    l.Registration!.Student != null && classIds.Contains(l.Registration.Student.ClassId!));

            // 5. So luong sinh vien da cham diem
            var gradedStudentsCount = await _context.Grades
                .Include(g => g.Registration).ThenInclude(r => r!.Student)
                .CountAsync(g => (g.InstructorScore.HasValue || g.FinalScore.HasValue) &&
                    g.Registration!.Student != null && classIds.Contains(g.Registration.Student.ClassId!));

            // 6. Lay SV moi dang ky gan day (top 5)
            var recentRegistrations = await _context.Registrations
                .Include(r => r.Student).ThenInclude(s => s!.User)
                .Include(r => r.Student).ThenInclude(s => s!.Class)
                .Include(r => r.Company)
                .Where(r => r.Student != null && classIds.Contains(r.Student.ClassId!))
                .OrderByDescending(r => r.CreatedDate)
                .Take(5)
                .ToListAsync();

            ViewBag.Lecturer = lecturer;
            ViewBag.MyClasses = myClasses;
            ViewBag.TotalStudents = totalStudents;
            ViewBag.RegisteredStudentsCount = registeredStudentsCount;
            ViewBag.PendingLogbooksCount = pendingLogbooksCount;
            ViewBag.GradedStudentsCount = gradedStudentsCount;
            ViewBag.RecentRegistrations = recentRegistrations;

            return View();
        }
    }
}