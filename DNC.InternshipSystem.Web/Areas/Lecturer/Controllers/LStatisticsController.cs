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
    public class LStatisticsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public LStatisticsController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);
            if (lecturer == null) return View();

            // 1. Tong so SV duoc phan cong
            var registrations = await _context.Registrations
                .Include(r => r.Student).ThenInclude(s => s!.User)
                .Include(r => r.Student).ThenInclude(s => s!.Class)
                .Include(r => r.Company)
                .Where(r => r.LecturerId == lecturer.UserId && r.Status == 1)
                .ToListAsync();

            // 2. Thong ke diem
            var grades = await _context.Grades
                .Include(g => g.Registration)
                .Where(g => g.Registration!.LecturerId == lecturer.UserId)
                .ToListAsync();

            int totalStudents = registrations.Count;
            int gradedCount = grades.Count(g => g.InstructorScore.HasValue);
            int pendingGrading = totalStudents - gradedCount;

            // 3. Phan bo diem
            var scoreDistribution = new int[5]; // 0-2, 2-4, 4-6, 6-8, 8-10
            foreach (var g in grades.Where(g => g.InstructorScore.HasValue))
            {
                double score = g.InstructorScore!.Value;
                if (score < 2) scoreDistribution[0]++;
                else if (score < 4) scoreDistribution[1]++;
                else if (score < 6) scoreDistribution[2]++;
                else if (score < 8) scoreDistribution[3]++;
                else scoreDistribution[4]++;
            }

            // 4. Thong ke logbook
            var totalLogbooks = await _context.Logbooks
                .Include(l => l.Registration)
                .CountAsync(l => l.Registration!.LecturerId == lecturer.UserId);

            var reviewedLogbooks = await _context.Logbooks
                .Include(l => l.Registration)
                .CountAsync(l => l.Registration!.LecturerId == lecturer.UserId && !string.IsNullOrEmpty(l.LecturerComment));

            // 5. Phan bo SV theo cong ty
            var companyGroups = registrations
                .GroupBy(r => r.Company?.Name ?? r.ExternalCompanyName ?? "Khác")
                .Select(g => new { Company = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(8)
                .ToList();

            ViewBag.TotalStudents = totalStudents;
            ViewBag.GradedCount = gradedCount;
            ViewBag.PendingGrading = pendingGrading;
            ViewBag.ScoreDistribution = scoreDistribution;
            ViewBag.TotalLogbooks = totalLogbooks;
            ViewBag.ReviewedLogbooks = reviewedLogbooks;
            ViewBag.CompanyGroups = companyGroups;
            ViewBag.Registrations = registrations;

            return View();
        }
    }
}
