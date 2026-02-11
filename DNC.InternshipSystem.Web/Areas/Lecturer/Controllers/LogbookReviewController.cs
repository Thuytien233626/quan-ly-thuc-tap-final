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
    public class LogbookReviewController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public LogbookReviewController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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
            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);

            if (registration!.LecturerId != lecturer!.UserId)
            {
                return Forbid();
            }

            logbook.LecturerComment = comment;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã lưu nhận xét." });
        }
    }
}
