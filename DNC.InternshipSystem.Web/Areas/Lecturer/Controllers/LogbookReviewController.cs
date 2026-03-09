using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DNC.InternshipSystem.Core.Entities;
using DNC.InternshipSystem.Infrastructure.Data;
using DNC.InternshipSystem.Web.Services;

namespace DNC.InternshipSystem.Web.Areas.Lecturer.Controllers
{
    [Area("Lecturer")]
    [Authorize(Roles = "Lecturer")]
    public class LogbookReviewController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly AIMentorService _aiService;
        private readonly ILogger<LogbookReviewController> _logger;

        public LogbookReviewController(AppDbContext context, UserManager<AppUser> userManager, AIMentorService aiService, ILogger<LogbookReviewController> logger)
        {
            _context = context;
            _userManager = userManager;
            _aiService = aiService;
            _logger = logger;
        }

        // GET: /Lecturer/LogbookReview?registrationId=xxx (hoac /Details/xxx)
        // O day minh dung id la RegistrationId
        public async Task<IActionResult> Details(Guid id)
        {
             var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });
            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);

            // Lay thong tin dang ky va logbook
            var registration = await _context.Registrations
                .Include(r => r.Student).ThenInclude(s => s!.User)
                .Include(r => r.Term)
                .FirstOrDefaultAsync(r => r.Id == id && r.LecturerId == lecturer!.UserId);

            if (registration == null)
            {
                // Neu khong tim thay theo ID truc tiep, thu tim sinh vien nao do co logbook can duyet
                if (id == Guid.Empty)
                {
                    return RedirectToAction("Index"); // Redirect ve danh sach chung
                }
                return NotFound();
            }

            var logbooks = await _context.Logbooks
                .Where(l => l.RegistrationId == registration.Id)
                .OrderBy(l => l.WeekNumber)
                .ToListAsync();

            ViewBag.Registration = registration;
            return View(logbooks);
        }

        // GET: /Lecturer/LogbookReview
        // Hien thi danh sach cac sinh vien co logbook can duyet (hoac tat ca)
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });
            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);

            // Lay danh sach sinh vien co logbook chua duyet
            var pendingLogbooks = await _context.Logbooks
                .Include(l => l.Registration).ThenInclude(r => r!.Student).ThenInclude(s => s!.User)
                .Include(l => l.Registration).ThenInclude(r => r!.Student).ThenInclude(s => s!.Class)
                .Where(l => l.Registration!.LecturerId == lecturer!.UserId && string.IsNullOrEmpty(l.LecturerComment))
                .OrderByDescending(l => l.SubmittedDate)
                .ToListAsync();

            // Group by Student/Registration de hien thi gon
            var grouped = pendingLogbooks
                .GroupBy(l => l.Registration)
                .Select(g => new 
                {
                    Registration = g.Key,
                    PendingCount = g.Count(),
                    LatestSubmission = g.Max(l => l.SubmittedDate)
                })
                .ToList();

            return View(grouped);
        }

        [HttpPost]
        public async Task<IActionResult> AddComment(Guid logbookId, string comment)
        {
            var logbook = await _context.Logbooks.FindAsync(logbookId);
            if (logbook == null) return NotFound();

            // Check quyen so huu (thong qua registration -> lecturer)
            var registration = await _context.Registrations.FindAsync(logbook.RegistrationId);
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);
            if (lecturer == null) return NotFound();

            if (registration?.LecturerId != lecturer.UserId)
            {
                return Forbid();
            }

            logbook.LecturerComment = comment;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã lưu nhận xét." });
        }

        // POST: /Lecturer/LogbookReview/AnalyzeWithAI
        [HttpPost]
        public async Task<IActionResult> AnalyzeWithAI(Guid logbookId)
        {
            try
            {
                var logbook = await _context.Logbooks
                    .Include(l => l.Registration)
                    .FirstOrDefaultAsync(l => l.Id == logbookId);

                if (logbook == null)
                    return Json(new { success = false, message = "Logbook không tồn tại." });

                // Verify lecturer owns this logbook
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                    return Json(new { success = false, message = "Bạn chưa đăng nhập." });

                var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);
                if (lecturer == null)
                    return Json(new { success = false, message = "Không tìm thấy thông tin giảng viên." });

                if (logbook.Registration?.LecturerId != lecturer.UserId)
                    return Json(new { success = false, message = "Bạn không có quyền truy cập logbook này." });

                _logger.LogInformation($"Analyzing logbook {logbookId} with AI");

                // Call AI to analyze
                var aiFeedback = await _aiService.AnalyzeLogbook(logbook.Content);

                // Update logbook with AI summary
                logbook.AISummary = aiFeedback;
                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = "Đã phân tích logbook với AI Mentor.",
                    feedback = aiFeedback
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing logbook with AI");
                return Json(new { 
                    success = false, 
                    message = "Lỗi khi phân tích với AI: " + ex.Message 
                });
            }
        }
    }
}
