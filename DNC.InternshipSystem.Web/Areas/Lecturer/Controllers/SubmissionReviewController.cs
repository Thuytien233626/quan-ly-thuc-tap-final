using DNC.InternshipSystem.Core.Entities;
using DNC.InternshipSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DNC.InternshipSystem.Web.Areas.Lecturer.Controllers
{
    [Area("Lecturer")]
    [Authorize(Roles = "Lecturer")]
    public class SubmissionReviewController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public SubmissionReviewController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Danh sach bao cao cua SV duoc phan cong
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);
            if (lecturer == null) return NotFound("Không tìm thấy giảng viên.");

            // Lay tat ca submission cua SV duoc phan cong cho GV nay
            var submissions = await _context.Submissions
                .Include(s => s.Registration)
                    .ThenInclude(r => r!.Student)
                        .ThenInclude(st => st!.User)
                .Include(s => s.Registration)
                    .ThenInclude(r => r!.Student)
                        .ThenInclude(st => st!.Class)
                .Include(s => s.Registration)
                    .ThenInclude(r => r!.Company)
                .Where(s => s.Registration!.LecturerId == lecturer.UserId)
                .OrderByDescending(s => s.SubmittedDate)
                .ToListAsync();

            var pending = submissions.Where(s => s.Status == "Pending").ToList();
            var reviewed = submissions.Where(s => s.Status != "Pending").ToList();

            ViewBag.PendingSubmissions = pending;
            ViewBag.ReviewedSubmissions = reviewed;

            return View();
        }

        // GET: Chi tiet bao cao
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);
            if (lecturer == null) return NotFound();

            var submission = await _context.Submissions
                .Include(s => s.Registration)
                    .ThenInclude(r => r!.Student)
                        .ThenInclude(st => st!.User)
                .Include(s => s.Registration)
                    .ThenInclude(r => r!.Student)
                        .ThenInclude(st => st!.Class)
                .Include(s => s.Registration)
                    .ThenInclude(r => r!.Company)
                .Include(s => s.Registration)
                    .ThenInclude(r => r!.Term)
                .FirstOrDefaultAsync(s => s.Id == id && s.Registration!.LecturerId == lecturer.UserId);

            if (submission == null) return NotFound("Không tìm thấy báo cáo.");

            return View(submission);
        }

        // POST: Duyet bao cao (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewSubmission(int submissionId, string status, string? comment)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);
            if (lecturer == null) return NotFound();

            var submission = await _context.Submissions
                .Include(s => s.Registration)
                .FirstOrDefaultAsync(s => s.Id == submissionId && s.Registration!.LecturerId == lecturer.UserId);

            if (submission == null)
                return Json(new { success = false, message = "Không tìm thấy báo cáo." });

            if (status != "Approved" && status != "Rejected")
                return Json(new { success = false, message = "Trạng thái không hợp lệ." });

            submission.Status = status;
            submission.LecturerComment = comment;
            submission.ReviewedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var msg = status == "Approved" ? "Đã duyệt báo cáo." : "Đã từ chối báo cáo.";
            return Json(new { success = true, message = msg });
        }

        // GET: Tai file bao cao
        public async Task<IActionResult> Download(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);
            if (lecturer == null) return NotFound();

            var submission = await _context.Submissions
                .Include(s => s.Registration)
                .FirstOrDefaultAsync(s => s.Id == id && s.Registration!.LecturerId == lecturer.UserId);

            if (submission == null) return NotFound();

            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", submission.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(fullPath)) return NotFound("File không tồn tại.");

            return PhysicalFile(fullPath, "application/octet-stream", Path.GetFileName(fullPath));
        }
    }
}
