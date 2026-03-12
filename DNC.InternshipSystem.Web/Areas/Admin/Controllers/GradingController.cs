using DNC.InternshipSystem.Core.Entities;
using DNC.InternshipSystem.Core.Enums;
using DNC.InternshipSystem.Core.Interfaces;
using DNC.InternshipSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DNC.InternshipSystem.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Lecturer")]
    public class GradingController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IGradingService _gradingService;

        public GradingController(
            AppDbContext context,
            UserManager<AppUser> userManager,
            IGradingService gradingService)
        {
            _context = context;
            _userManager = userManager;
            _gradingService = gradingService;
        }

        // GET: /Admin/Grading — Danh sach sinh vien can cham diem
        public async Task<IActionResult> Index(int? majorId = null, string? search = null)
        {
            var user = await _userManager.GetUserAsync(User);
            var isLecturer = User.IsInRole("Lecturer");

            ViewBag.CurrentMajorId = majorId;
            ViewBag.CurrentSearch = search;

            ViewBag.Majors = await _context.Majors
                .GroupBy(m => m.Name)
                .Select(g => g.First())
                .ToListAsync();

            var query = _context.Registrations
                .Include(r => r.Student).ThenInclude(s => s!.User)
                .Include(r => r.Student).ThenInclude(s => s!.Class).ThenInclude(c => c!.Major)
                .Include(r => r.Company)
                .Include(r => r.Lecturer).ThenInclude(l => l!.User)
                .Where(r => r.Status == RegistrationStatus.Approved && r.LecturerId != null);

            // GV chi thay sinh vien minh phu trach
            if (isLecturer && user != null)
                query = query.Where(r => r.LecturerId == user.Id);

            if (majorId.HasValue)
            {
                var selectedMajor = await _context.Majors.FindAsync(majorId);
                if (selectedMajor != null)
                    query = query.Where(r => r.Student!.Class!.Major!.Name == selectedMajor.Name);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(r =>
                    r.Student!.User!.FullName.Contains(search) ||
                    r.Student.StudentCode.Contains(search));
            }

            var students = await query.ToListAsync();

            // Tinh lai diem tong ket de hien thi
            foreach (var s in students)
            {
                s.FinalScore = _gradingService.CalculateFinalScore(s.CompanyScore, s.InstructorScore);
            }

            return View(students);
        }

        // POST: Cap nhat diem (AJAX) — Da them ValidateAntiForgeryToken
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateScores(Guid id, double? companyScore, double? instructorScore)
        {
            // Kiem tra quyen GV neu la Lecturer
            if (User.IsInRole("Lecturer"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var registration = await _context.Registrations.FindAsync(id);
                if (registration?.LecturerId.ToString() != userId)
                {
                    return Json(new { success = false, message = "Bạn không được phân công hướng dẫn sinh viên này." });
                }
            }

            var result = await _gradingService.UpdateScoresFromAdmin(id, companyScore, instructorScore);
            return Json(new { success = result.Success, finalScore = result.Data, message = result.Message });
        }

        // GET: Xuat bang diem Excel
        public async Task<IActionResult> Export(int? majorId = null, string? search = null)
        {
            var user = await _userManager.GetUserAsync(User);
            var isLecturer = User.IsInRole("Lecturer");

            Guid? lecturerId = isLecturer && user != null ? user.Id : null;
            var excelBytes = await _gradingService.ExportToExcel(lecturerId, majorId, search);

            string excelName = $"KetQuaThucTap-{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            return File(excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                excelName);
        }
    }
}