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
        public async Task<IActionResult> SubmitReport(string ReportType, string Title, string Note, IFormFile File)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "Account", new { area = "" });

            // tìm registration
            var registration = await _context.Registrations
                .Where(r => r.StudentId == user.Id)
                .OrderByDescending(r => r.CreatedDate)
                .FirstOrDefaultAsync();

            if (registration == null)
            {
                TempData["Error"] = "Bạn chưa đăng ký thực tập.";
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
                Title = Title,
                Type = ReportType,
                FilePath = "/uploads/reports/" + fileName,
                Note = Note ?? "",
                SubmittedDate = DateTime.Now,
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

            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", path.TrimStart('/'));

            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            var contentType = "application/octet-stream";

            return PhysicalFile(fullPath, contentType, Path.GetFileName(fullPath));
        }
    }
}