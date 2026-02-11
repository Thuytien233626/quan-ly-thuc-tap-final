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

            // 1. Tim giang vien hien tai
            var lecturer = await _context.Lecturers
                .Include(l => l.User)
                .FirstOrDefaultAsync(l => l.UserId == user.Id);

            if (lecturer == null)
            {
                ViewBag.Message = "Hồ sơ giảng viên chưa được tạo.";
                return View();
            }

            // 2. Lay cac thong ke cho Dashboard
            // a. So luong sinh vien dang huong dan (Registration co LecturerId = lecturer.UserId va Status = 1 (Da duyet))
            var assignedStudentsCount = await _context.Registrations
                .CountAsync(r => r.LecturerId == lecturer.UserId && r.Status == 1);

            // b. So luong logbook cho duyet (Logbook thuoc Registration cua GV nay, chua co Comment)
            // Luu y: Logbook linked to Registration, Registration linked to Lecturer
            var pendingLogbooksCount = await _context.Logbooks
                .Include(l => l.Registration)
                .CountAsync(l => l.Registration!.LecturerId == lecturer.UserId 
                              && string.IsNullOrEmpty(l.LecturerComment));

            // c. So luong sinh vien da cham diem (Co Grade record)
            var gradedStudentsCount = await _context.Grades
                .Include(g => g.Registration)
                .CountAsync(g => g.Registration!.LecturerId == lecturer.UserId 
                              && (g.InstructorScore.HasValue || g.FinalScore.HasValue));

            // d. Lay danh sach sinh vien moi nhat (top 5)
            var recentStudents = await _context.Registrations
                .Include(r => r.Student).ThenInclude(s => s!.User)
                .Include(r => r.Student).ThenInclude(s => s!.Class)
                .Include(r => r.Company)
                .Where(r => r.LecturerId == lecturer.UserId && r.Status == 1)
                .OrderByDescending(r => r.UpdatedDate) // Hien thi nhung nguoi moi dc update trang thai
                .Take(5)
                .ToListAsync();

            ViewBag.Lecturer = lecturer;
            ViewBag.AssignedStudentsCount = assignedStudentsCount;
            ViewBag.PendingLogbooksCount = pendingLogbooksCount;
            ViewBag.GradedStudentsCount = gradedStudentsCount;
            ViewBag.RecentStudents = recentStudents;

            return View();
        }
    }
}