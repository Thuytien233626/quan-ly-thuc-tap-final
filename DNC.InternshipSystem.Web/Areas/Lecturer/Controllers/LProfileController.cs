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
                // Lay danh sach lop GV phu trach
                var classIds = await _context.Classes
                    .Where(c => c.LecturerId == lecturer.UserId)
                    .Select(c => c.Id)
                    .ToListAsync();

                // Tong SV trong cac lop phu trach
                assignedStudents = await _context.Students
                    .CountAsync(s => classIds.Contains(s.ClassId));

                // Lay registration IDs (approved) de query logbook/grade
                var regIds = await _context.Registrations
                    .Include(r => r.Student)
                    .Where(r => r.Status == RegistrationStatus.Approved &&
                        (r.LecturerId == lecturer.UserId || classIds.Contains(r.Student!.ClassId)))
                    .Select(r => r.Id)
                    .ToListAsync();

                totalLogbooks = await _context.Logbooks
                    .CountAsync(l => regIds.Contains(l.RegistrationId));

                gradedStudents = await _context.Grades
                    .CountAsync(g => g.InstructorScore.HasValue && regIds.Contains(g.RegistrationId));
            }

            ViewBag.Lecturer = lecturer;
            ViewBag.AssignedStudents = assignedStudents;
            ViewBag.TotalLogbooks = totalLogbooks;
            ViewBag.GradedStudents = gradedStudents;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAvatar(IFormFile avatar)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            if (avatar == null || avatar.Length == 0)
            {
                TempData["Error"] = "Vui lòng chọn ảnh.";
                return RedirectToAction("Index");
            }

            if (avatar.Length > 2 * 1024 * 1024)
            {
                TempData["Error"] = "Ảnh không được vượt quá 2MB.";
                return RedirectToAction("Index");
            }

            var allowedTypes = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(avatar.FileName).ToLower();
            if (!allowedTypes.Contains(ext))
            {
                TempData["Error"] = "Chỉ hỗ trợ ảnh JPG, PNG, WEBP.";
                return RedirectToAction("Index");
            }

            var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars");
            if (!Directory.Exists(uploadDir))
                Directory.CreateDirectory(uploadDir);

            // Xoa avatar cu neu co
            if (!string.IsNullOrEmpty(user.AvatarUrl))
            {
                var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.AvatarUrl.TrimStart('/'));
                if (System.IO.File.Exists(oldPath))
                    System.IO.File.Delete(oldPath);
            }

            var fileName = $"avatar_{user.Id}{ext}";
            var filePath = Path.Combine(uploadDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await avatar.CopyToAsync(stream);
            }

            user.AvatarUrl = $"/uploads/avatars/{fileName}";
            await _userManager.UpdateAsync(user);

            TempData["Success"] = "Cập nhật ảnh đại diện thành công!";
            return RedirectToAction("Index");
        }
    }
}
