using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DNC.InternshipSystem.Core.Entities;
using DNC.InternshipSystem.Core.Enums;
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

        // ===============================
        // INDEX
        // ===============================
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "Account", new { area = "" });

            // tìm registration mới nhất
            var registration = await _context.Registrations
                .Include(r => r.Term)
                .Where(r => r.StudentId == user.Id)
                .OrderByDescending(r => r.CreatedDate)
                .FirstOrDefaultAsync();

            int logbookCount = 0;
            int totalWeeks = 0;

            if (registration != null)
            {
                logbookCount = await _context.Logbooks
                    .CountAsync(l => l.RegistrationId == registration.Id);

                totalWeeks = registration.Term?.DurationInWeeks ?? 0;
            }

            // lấy danh sách submission
            var submissions = new List<Submission>();

            if (registration != null)
            {
                submissions = await _context.Submissions
                    .Where(s => s.RegistrationId == registration.Id)
                    .OrderByDescending(s => s.SubmittedDate)
                    .ToListAsync();
            }

            ViewBag.Registration = registration;
            ViewBag.HasRegistration = registration != null;
            ViewBag.IsApproved = registration?.Status == RegistrationStatus.Approved;
            ViewBag.RegistrationStatus = registration?.Status;
            ViewBag.LogbookCount = logbookCount;
            ViewBag.TotalWeeks = totalWeeks;
            ViewBag.Submissions = submissions;

            return View();
        }

        // ===============================
        // SUBMIT REPORT
        // ===============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReport(string Title, string Note, IFormFile File)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "Account", new { area = "" });

            // tìm registration
            var registration = await _context.Registrations
                .Where(r => r.StudentId == user.Id)
                .OrderByDescending(r => r.CreatedDate)
                .FirstOrDefaultAsync();

            if (registration == null || registration.Status != RegistrationStatus.Approved)
            {
                TempData["Error"] = "Bạn chưa đăng ký thực tập hoặc đơn chưa được duyệt.";
                return RedirectToAction("Index");
            }

            // Kiem tra da nop bao cao chua
            var existingSubmission = await _context.Submissions
                .AnyAsync(s => s.RegistrationId == registration.Id && s.Type == "Báo cáo thực tập");

            if (existingSubmission)
            {
                TempData["Error"] = "Bạn đã nộp báo cáo thực tập rồi. Nếu cần cập nhật, vui lòng liên hệ giảng viên.";
                return RedirectToAction("Index");
            }

            if (File == null || File.Length == 0)
            {
                TempData["Error"] = "Vui lòng chọn file.";
                return RedirectToAction("Index");
            }

            // kiểm tra dung lượng file (10MB)
            if (File.Length > 10 * 1024 * 1024)
            {
                TempData["Error"] = "File vượt quá 10MB.";
                return RedirectToAction("Index");
            }

            // kiểm tra định dạng file
            var allowedExtensions = new[] { ".pdf", ".docx", ".xlsx" };
            var extension = Path.GetExtension(File.FileName).ToLower();

            if (!allowedExtensions.Contains(extension))
            {
                TempData["Error"] = "Chỉ cho phép file PDF, DOCX, XLSX.";
                return RedirectToAction("Index");
            }

            // tạo folder uploads nếu chưa có
            var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/reports");

            if (!Directory.Exists(uploadFolder))
                Directory.CreateDirectory(uploadFolder);

            // tạo tên file unique
            var fileName = Guid.NewGuid().ToString() + extension;

            var filePath = Path.Combine(uploadFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await File.CopyToAsync(stream);
            }

            // lưu database
            var submission = new Submission
            {
                RegistrationId = registration.Id,
                Title = Title ?? "Báo cáo thực tập",
                Type = "Báo cáo thực tập",
                FilePath = "/uploads/reports/" + fileName,
                Note = Note ?? "",
                SubmittedDate = DateTime.UtcNow,
                Status = "Pending"
            };

            _context.Submissions.Add(submission);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Nộp báo cáo thành công!";

            return RedirectToAction("Index");
        }

        // ===============================
        // DOWNLOAD FILE
        // ===============================
        [HttpGet]
        public IActionResult Download(string path)
        {
            if (string.IsNullOrEmpty(path))
                return NotFound();

            // Chan tan cong Path Traversal: chi cho download trong thu muc uploads
            var uploadsRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads"));
            var fullPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", path.TrimStart('/')));

            if (!fullPath.StartsWith(uploadsRoot))
                return Forbid();

            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            var contentType = "application/octet-stream";
            return PhysicalFile(fullPath, contentType, Path.GetFileName(fullPath));
        }

        // ===============================
        // UPDATE REPORT (nop lai)
        // ===============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateReport(int id, string Title, string Note, IFormFile? File)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var submission = await _context.Submissions
                .Include(s => s.Registration)
                .FirstOrDefaultAsync(s => s.Id == id && s.Registration!.StudentId == user.Id);

            if (submission == null)
            {
                TempData["Error"] = "Không tìm thấy báo cáo.";
                return RedirectToAction("Index");
            }

            if (submission.Status != "Pending")
            {
                TempData["Error"] = "Báo cáo đã được duyệt, không thể chỉnh sửa.";
                return RedirectToAction("Index");
            }

            submission.Title = Title ?? submission.Title;
            submission.Note = Note ?? "";

            // Neu co file moi → thay the
            if (File != null && File.Length > 0)
            {
                if (File.Length > 10 * 1024 * 1024)
                {
                    TempData["Error"] = "File vượt quá 10MB.";
                    return RedirectToAction("Index");
                }

                var allowedExtensions = new[] { ".pdf", ".docx", ".xlsx" };
                var extension = Path.GetExtension(File.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                {
                    TempData["Error"] = "Chỉ cho phép file PDF, DOCX, XLSX.";
                    return RedirectToAction("Index");
                }

                // Xoa file cu
                var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", submission.FilePath.TrimStart('/'));
                if (System.IO.File.Exists(oldPath))
                    System.IO.File.Delete(oldPath);

                // Luu file moi
                var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/reports");
                if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

                var fileName = Guid.NewGuid().ToString() + extension;
                var filePath = Path.Combine(uploadFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await File.CopyToAsync(stream);
                }
                submission.FilePath = "/uploads/reports/" + fileName;
            }

            submission.SubmittedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật báo cáo thành công!";
            return RedirectToAction("Index");
        }

        // ===============================
        // DELETE REPORT (huy bao cao)
        // ===============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReport(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var submission = await _context.Submissions
                .Include(s => s.Registration)
                .FirstOrDefaultAsync(s => s.Id == id && s.Registration!.StudentId == user.Id);

            if (submission == null)
            {
                TempData["Error"] = "Không tìm thấy báo cáo.";
                return RedirectToAction("Index");
            }

            if (submission.Status != "Pending")
            {
                TempData["Error"] = "Báo cáo đã được duyệt, không thể hủy.";
                return RedirectToAction("Index");
            }

            // Xoa file
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", submission.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);

            _context.Submissions.Remove(submission);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã hủy báo cáo thành công!";
            return RedirectToAction("Index");
        }
    }
}