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
    public class LogbookController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public LogbookController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Student/Logbook
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            // Tim registration cua sinh vien (moi nhat, da duyet)
            var registration = await _context.Registrations
                .Include(r => r.Term)
                .Where(r => r.StudentId == user.Id)
                .OrderByDescending(r => r.CreatedDate)
                .FirstOrDefaultAsync();

            if (registration != null)
            {
                // Lay danh sach logbook
                var logbooks = await _context.Logbooks
                    .Where(l => l.RegistrationId == registration.Id)
                    .OrderBy(l => l.WeekNumber)
                    .ToListAsync();
                

                int totalWeeks = registration.Term?.DurationInWeeks ?? 0;
                int submittedWeeks = logbooks.Count;
                var currentWeek = 0;

if (registration != null && registration.Term != null)
{
    var start = registration.Term.InternshipStart;

    var diffDays = (DateTime.Now - start).Days;

    if (diffDays >= 0)
        currentWeek = diffDays / 7 + 1;
}

ViewBag.CurrentWeek = currentWeek;
                ViewBag.Registration = registration;
                ViewBag.Logbooks = logbooks;
                ViewBag.TotalWeeks = totalWeeks;
                ViewBag.SubmittedWeeks = submittedWeeks;
                ViewBag.HasRegistration = true;
            }
            else
            {
                ViewBag.HasRegistration = false;
                ViewBag.Logbooks = new List<Logbook>();
                ViewBag.TotalWeeks = 0;
                ViewBag.SubmittedWeeks = 0;
            }

            return View();
        }
        [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CreateLogbook(int WeekNumber, DateTime StartDate, DateTime EndDate, string Content)
{
    var user = await _userManager.GetUserAsync(User);

    if (user == null)
        return RedirectToAction("Login", "Account", new { area = "" });

    var registration = await _context.Registrations
        .Where(r => r.StudentId == user.Id)
        .OrderByDescending(r => r.CreatedDate)
        .FirstOrDefaultAsync();

    if (registration == null)
        return RedirectToAction("Index");

    // kiểm tra tuần đã tồn tại chưa
    var existed = await _context.Logbooks
        .AnyAsync(l => l.RegistrationId == registration.Id && l.WeekNumber == WeekNumber);

    if (existed)
    {
        TempData["Error"] = "Tuần này đã có nhật ký.";
        return RedirectToAction("Index");
    }

    var logbook = new Logbook
    {
        RegistrationId = registration.Id,
        WeekNumber = WeekNumber,
        StartDate = StartDate,
        EndDate = EndDate,
        Content = Content,
        SubmittedDate = DateTime.Now
    };

    _context.Logbooks.Add(logbook);

    await _context.SaveChangesAsync();

    TempData["Success"] = "Lưu nhật ký thành công!";

    return RedirectToAction("Index");
}
    }
}
