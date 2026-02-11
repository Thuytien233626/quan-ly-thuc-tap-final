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
    public class MyStudentsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public MyStudentsController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Lecturer/MyStudents
        public async Task<IActionResult> Index(string search = "", int page = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            // 1. Tim GV hien tai
            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);
            if (lecturer == null) return View("Error");

            // 2. Query sinh vien duoc phan cong (da duyet Status=1)
            var query = _context.Registrations
                .Include(r => r.Student).ThenInclude(s => s!.User)
                .Include(r => r.Student).ThenInclude(s => s!.Class)
                .Include(r => r.Company)
                .Where(r => r.LecturerId == lecturer.UserId && r.Status == 1)
                .AsQueryable();

            // 3. Tim kiem
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(r => 
                    r.Student!.User!.FullName.ToLower().Contains(search) || 
                    r.Student.StudentCode.Contains(search));
            }

            // 4. Phan trang
            int pageSize = 10;
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            
            var students = await query
                .OrderBy(r => r.Student!.StudentCode)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Search = search;
            
            return View(students);
        }

        // GET: /Lecturer/MyStudents/Details/{studentId}
        public async Task<IActionResult> Details(Guid id)
        {
             var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });
            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);

            // Lay thong tin dang ky cua sinh vien do (phai thuoc GV nay)
            var registration = await _context.Registrations
                .Include(r => r.Student).ThenInclude(s => s!.User)
                .Include(r => r.Student).ThenInclude(s => s!.Class)
                .Include(r => r.Company)
                .Include(r => r.Term)
                .FirstOrDefaultAsync(r => r.StudentId == id && r.LecturerId == lecturer!.UserId);

            if (registration == null)
            {
                return NotFound();
            }

            // Lay diem
            var grade = await _context.Grades
                .FirstOrDefaultAsync(g => g.RegistrationId == registration.Id);

            // Lay logbook
            var logbooks = await _context.Logbooks
                .Where(l => l.RegistrationId == registration.Id)
                .OrderBy(l => l.WeekNumber)
                .ToListAsync();

            ViewBag.Grade = grade;
            ViewBag.Logbooks = logbooks;

            return View(registration);
        }
    }
}
