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

        public LogbookController(
            ILogbookService logbookService,
            UserManager<AppUser> userManager,
            AppDbContext context)
        {
            _logbookService = logbookService;
            _userManager = userManager;
            _context = context;
        }

        // GET: /Student/Logbook — Trang tong quan nhat ky thuc tap
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            // Lay don dang ky moi nhat cua sinh vien
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

        // POST: /Student/Logbook/CreateLogbook — Tao nhat ky tuan moi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLogbook(int WeekNumber, DateTime StartDate, DateTime EndDate, string Content)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account", new { area = "" });

            var result = await _logbookService.CreateLogbook(user.Id, WeekNumber, StartDate, EndDate, Content);

            if (result.Success)
                TempData["Success"] = result.Message;
            else
                TempData["Error"] = result.Message;

            return RedirectToAction("Index");
        }
    }
}
