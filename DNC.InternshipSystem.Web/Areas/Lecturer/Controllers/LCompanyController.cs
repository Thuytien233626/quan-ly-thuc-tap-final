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
    public class LCompanyController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public LCompanyController(AppDbContext context, UserManager<AppUser> userManager)
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

            // Lay danh sach cong ty ma SV cua GV dang thuc tap
            var registrations = await _context.Registrations
                .Include(r => r.Company)
                .Include(r => r.Student).ThenInclude(s => s!.User)
                .Where(r => r.LecturerId == lecturer.UserId && r.Status == 1)
                .ToListAsync();

            // Group by company
            var companyGroups = registrations
                .GroupBy(r => new 
                { 
                    Id = r.CompanyId ?? 0,
                    Name = r.Company?.Name ?? r.ExternalCompanyName ?? "Không xác định",
                    Address = r.Company?.Address ?? r.ExternalCompanyAddress ?? "",
                    Phone = r.Company?.PhoneNumber ?? "",
                    Province = r.Company?.Province ?? ""
                })
                .Select(g => new 
                {
                    g.Key.Id,
                    g.Key.Name,
                    g.Key.Address,
                    g.Key.Phone,
                    g.Key.Province,
                    StudentCount = g.Count(),
                    Students = g.Select(r => r.Student).ToList()
                })
                .OrderByDescending(x => x.StudentCount)
                .ToList();

            return View(companyGroups);
        }
    }
}
