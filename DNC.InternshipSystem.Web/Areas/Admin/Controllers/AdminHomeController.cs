using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using DNC.InternshipSystem.Web.Areas.Admin.Models;

namespace DNC.InternshipSystem.Web.Areas.Admin.Controllers
{
    [Area("Admin")] 
    [Authorize(Roles = "Admin")] 
    public class AdminHomeController : Controller
    {
        private readonly DNC.InternshipSystem.Infrastructure.Data.AppDbContext _context;

        public AdminHomeController(DNC.InternshipSystem.Infrastructure.Data.AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Thong ke he thong
            var activeTermCount = await _context.InternshipTerms.CountAsync(t => t.IsActive);
            var totalStudents = await _context.Students.CountAsync();
            var totalLecturers = await _context.Lecturers.CountAsync();
            var totalCompanies = await _context.Companies.CountAsync();

            // SV dang thuc tap (co Registration approved & co GVHD)
            var activeStudents = await _context.Registrations
                .CountAsync(r => r.Status == 1 && r.LecturerId != null);
            
            // GV dang huong dan (distinct lecturers assigned)
            var activeLecturers = await _context.Registrations
                .Where(r => r.Status == 1 && r.LecturerId != null)
                .Select(r => r.LecturerId)
                .Distinct()
                .CountAsync();
            
            // Don cho duyet
            var pendingRegistrations = await _context.Registrations
                .CountAsync(r => r.Status == 0);

            // Thong ke diem so
            var gradedRegistrations = await _context.Registrations
                .Where(r => r.Status == 1 && r.LecturerId != null)
                .Select(r => r.FinalScore)
                .ToListAsync();

            int passedCount = gradedRegistrations.Count(s => s.HasValue && s.Value >= 5.0);
            int failedCount = gradedRegistrations.Count(s => s.HasValue && s.Value < 5.0);
            int notGradedCount = gradedRegistrations.Count(s => !s.HasValue);

            // Phan bo diem so: 0-2, 2-4, 4-6, 6-8, 8-10
            var scored = gradedRegistrations.Where(s => s.HasValue).Select(s => s!.Value).ToList();
            var scoreDistribution = new int[5];
            foreach (var score in scored)
            {
                if (score < 2) scoreDistribution[0]++;
                else if (score < 4) scoreDistribution[1]++;
                else if (score < 6) scoreDistribution[2]++;
                else if (score < 8) scoreDistribution[3]++;
                else scoreDistribution[4]++;
            }

            // Cac dang ky gan day
            var recentRegistrations = await _context.Registrations
                .Include(r => r.Student).ThenInclude(s => s!.User)
                .Include(r => r.Student).ThenInclude(s => s!.Class).ThenInclude(c => c!.Major)
                .Include(r => r.Company)
                .OrderByDescending(r => r.CreatedDate)
                .Take(5)
                .Select(r => new RecentRegistrationItem
                {
                    StudentName = r.Student!.User!.FullName,
                    StudentCode = r.Student.StudentCode,
                    CompanyName = r.Company != null ? r.Company.Name : (r.ExternalCompanyName ?? ""),
                    MajorName = r.Student.Class != null && r.Student.Class.Major != null ? r.Student.Class.Major.Name : "",
                    CreatedDate = r.CreatedDate,
                    Status = r.Status
                })
                .ToListAsync();

            var model = new DashboardViewModel
            {
                ActiveTermCount = activeTermCount,
                TotalStudents = totalStudents,
                ActiveStudents = activeStudents,
                TotalLecturers = totalLecturers,
                ActiveLecturers = activeLecturers,
                TotalCompanies = totalCompanies,
                PendingRegistrations = pendingRegistrations,
                PassedCount = passedCount,
                FailedCount = failedCount,
                NotGradedCount = notGradedCount,
                ScoreDistribution = scoreDistribution,
                RecentRegistrations = recentRegistrations
            };

            return View(model);
        }
    }
}