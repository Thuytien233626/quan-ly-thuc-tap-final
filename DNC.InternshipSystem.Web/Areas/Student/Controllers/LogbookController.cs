using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DNC.InternshipSystem.Core.Entities;
using DNC.InternshipSystem.Core.Enums;
using DNC.InternshipSystem.Core.Interfaces;
using DNC.InternshipSystem.Infrastructure.Data;

namespace DNC.InternshipSystem.Web.Areas.Student.Controllers
{
    [Area("Student")]
    [Authorize(Roles = "Student")]
    public class LogbookController : Controller
    {
        private readonly ILogbookService _logbookService;
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public LogbookController(
            ILogbookService logbookService,
            UserManager<AppUser> userManager,
            AppDbContext context,
            IWebHostEnvironment env)
        {
            _logbookService = logbookService;
            _userManager = userManager;
            _context = context;
            _env = env;
        }

        // GET: /Student/Logbook
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var registration = await _logbookService.GetActiveRegistration(user.Id);

            if (registration != null)
            {
                bool isApproved = registration.Status == RegistrationStatus.Approved;
                ViewBag.IsApproved = isApproved;
                ViewBag.RegistrationStatus = registration.Status;

                if (isApproved)
                {
                    var logbooks = await _logbookService.GetLogbooks(registration.Id);
                    int totalWeeks = registration.Term?.DurationInWeeks ?? 0;
                    int submittedWeeks = logbooks.Count;

                    int currentWeek = 0;
                    if (registration.Term != null)
                    {
                        var start = registration.Term.InternshipStart;
                        var diffDays = (DateTime.UtcNow - start).Days;
                        if (diffDays >= 0)
                            currentWeek = diffDays / 7 + 1;
                    }

                    ViewBag.CurrentWeek = currentWeek;
                    ViewBag.Registration = registration;
                    ViewBag.Logbooks = logbooks;
                    ViewBag.TotalWeeks = totalWeeks;
                    ViewBag.SubmittedWeeks = submittedWeeks;
                }
                else
                {
                    ViewBag.Logbooks = new List<Logbook>();
                    ViewBag.TotalWeeks = 0;
                    ViewBag.SubmittedWeeks = 0;
                }

                ViewBag.HasRegistration = true;
            }
            else
            {
                ViewBag.HasRegistration = false;
                ViewBag.IsApproved = false;
                ViewBag.Logbooks = new List<Logbook>();
                ViewBag.TotalWeeks = 0;
                ViewBag.SubmittedWeeks = 0;
            }

            return View();
        }

        // POST: /Student/Logbook/CreateLogbook
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLogbook(int WeekNumber, DateTime StartDate, DateTime EndDate, string Content, IFormFile? EvidenceFile)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account", new { area = "" });

            // Upload file if provided
            string? evidenceUrl = await SaveUploadedFile(EvidenceFile);

            var result = await _logbookService.CreateLogbook(user.Id, WeekNumber, StartDate, EndDate, Content);

            // Update evidence URL if file was uploaded
            if (result.Success && evidenceUrl != null)
            {
                var registration = await _logbookService.GetActiveRegistration(user.Id);
                if (registration != null)
                {
                    var logbook = await _context.Logbooks
                        .Where(l => l.RegistrationId == registration.Id && l.WeekNumber == WeekNumber)
                        .FirstOrDefaultAsync();
                    if (logbook != null)
                    {
                        logbook.EvidenceImageUrl = evidenceUrl;
                        await _context.SaveChangesAsync();
                    }
                }
            }

            if (result.Success)
                TempData["Success"] = result.Message;
            else
                TempData["Error"] = result.Message;

            return RedirectToAction("Index");
        }

        // POST: /Student/Logbook/UpdateLogbook
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateLogbook(Guid LogbookId, string Content, IFormFile? EvidenceFile)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account", new { area = "" });

            string? evidenceUrl = await SaveUploadedFile(EvidenceFile);

            // Keep existing evidence if no new file
            if (evidenceUrl == null)
            {
                var existing = await _context.Logbooks.FindAsync(LogbookId);
                evidenceUrl = existing?.EvidenceImageUrl;
            }

            var result = await _logbookService.UpdateLogbook(LogbookId, user.Id, Content, evidenceUrl);

            if (result.Success)
                TempData["Success"] = result.Message;
            else
                TempData["Error"] = result.Message;

            return RedirectToAction("Index");
        }

        // POST: /Student/Logbook/DeleteLogbook
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLogbook(Guid LogbookId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account", new { area = "" });

            var result = await _logbookService.DeleteLogbook(LogbookId, user.Id);

            if (result.Success)
                TempData["Success"] = result.Message;
            else
                TempData["Error"] = result.Message;

            return RedirectToAction("Index");
        }

        // POST: /Student/Logbook/RequestResubmit (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestResubmit(Guid logbookId, string reason)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var result = await _logbookService.StudentRequestResubmit(logbookId, user.Id, reason);
            return Json(new { success = result.Success, message = result.Message });
        }

        private async Task<string?> SaveUploadedFile(IFormFile? file)
        {
            if (file == null || file.Length == 0) return null;

            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "logbooks");
            Directory.CreateDirectory(uploadsDir);

            var ext = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadsDir, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/uploads/logbooks/{fileName}";
        }
    }
}
