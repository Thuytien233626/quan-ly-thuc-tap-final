using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DNC.InternshipSystem.Core.Entities;
using DNC.InternshipSystem.Core.Interfaces;
using DNC.InternshipSystem.Infrastructure.Data;

namespace DNC.InternshipSystem.Web.Areas.Lecturer.Controllers
{
    [Area("Lecturer")]
    [Authorize(Roles = "Lecturer")]
    public class LogbookReviewController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogbookService _logbookService;

        public LogbookReviewController(
            AppDbContext context,
            UserManager<AppUser> userManager,
            ILogbookService logbookService)
        {
            _context = context;
            _userManager = userManager;
            _logbookService = logbookService;
        }

        // GET: /Lecturer/LogbookReview/Details/{id} — Xem chi tiet logbook cua 1 SV
        public async Task<IActionResult> Details(Guid id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });
            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);

            // Lay danh sach lop GV phu trach
            var lecClassIds = await _context.Classes
                .Where(c => c.LecturerId == lecturer!.UserId)
                .Select(c => c.Id)
                .ToListAsync();

            // Lay thong tin don dang ky (phan cong truc tiep hoac qua lop)
            var registration = await _context.Registrations
                .Include(r => r.Student).ThenInclude(s => s!.User)
                .Include(r => r.Student).ThenInclude(s => s!.Class)
                .Include(r => r.Company)
                .Include(r => r.Term)
                .FirstOrDefaultAsync(r => r.Id == id &&
                    (r.LecturerId == lecturer!.UserId || lecClassIds.Contains(r.Student!.ClassId)));

            if (registration == null)
            {
                if (id == Guid.Empty)
                    return RedirectToAction("Index");
                return NotFound();
            }

            // Lay danh sach logbook cua SV
            var logbooks = await _logbookService.GetLogbooks(registration.Id);

            ViewBag.Registration = registration;
            return View(logbooks);
        }

        // GET: /Lecturer/LogbookReview
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });
            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);

            // Lay tat ca logbook cua SV do GV phu trach
            var allLogbooks = await _context.Logbooks
                .Include(l => l.Registration).ThenInclude(r => r!.Student).ThenInclude(s => s!.User)
                .Include(l => l.Registration).ThenInclude(r => r!.Student).ThenInclude(s => s!.Class)
                .Include(l => l.Registration).ThenInclude(r => r!.Company)
                .Where(l => l.Registration!.LecturerId == lecturer!.UserId)
                .OrderByDescending(l => l.SubmittedDate)
                .ToListAsync();

            // Nhom chua duyet
            var pending = allLogbooks
                .Where(l => string.IsNullOrEmpty(l.LecturerComment))
                .GroupBy(l => l.Registration)
                .Select(g => new
                {
                    Registration = g.Key,
                    PendingCount = g.Count(),
                    LatestSubmission = g.Max(l => l.SubmittedDate)
                })
                .ToList();

            // Nhom da duyet
            var reviewed = allLogbooks
                .Where(l => !string.IsNullOrEmpty(l.LecturerComment))
                .GroupBy(l => l.Registration)
                .Select(g => new
                {
                    Registration = g.Key,
                    ReviewedCount = g.Count(),
                    TotalCount = allLogbooks.Count(l => l.RegistrationId == g.Key!.Id),
                    LatestReview = g.Max(l => l.SubmittedDate)
                })
                .ToList();

            ViewBag.ReviewedList = reviewed;
            return View(pending);
        }

        // POST: Them nhan xet vao logbook (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(Guid logbookId, string comment)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);
            if (lecturer == null) return NotFound();

            var result = await _logbookService.AddComment(logbookId, lecturer.UserId, comment);
            return Json(new { success = result.Success, message = result.Message });
        }

        // POST: Phan tich logbook voi AI Mentor (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AnalyzeWithAI(Guid logbookId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false, message = "Bạn chưa đăng nhập." });

            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);
            if (lecturer == null) return Json(new { success = false, message = "Không tìm thấy thông tin giảng viên." });

            var result = await _logbookService.AnalyzeWithAI(logbookId, lecturer.UserId);
            return Json(new { success = result.Success, message = result.Message, feedback = result.Data });
        }

        // POST: Yeu cau SV nop lai (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestResubmit(Guid logbookId, string reason)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);
            if (lecturer == null) return NotFound();

            var result = await _logbookService.RequestResubmit(logbookId, lecturer.UserId, reason ?? "");
            return Json(new { success = result.Success, message = result.Message });
        }

        // POST: Chap nhan yeu cau nop lai cua SV (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveResubmit(Guid logbookId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);
            if (lecturer == null) return NotFound();

            var result = await _logbookService.ApproveResubmitRequest(logbookId, lecturer.UserId);
            return Json(new { success = result.Success, message = result.Message });
        }

        // POST: Tu choi yeu cau nop lai cua SV (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectResubmit(Guid logbookId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);
            if (lecturer == null) return NotFound();

            var result = await _logbookService.RejectResubmitRequest(logbookId, lecturer.UserId);
            return Json(new { success = result.Success, message = result.Message });
        }
    }
}
