using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DNC.InternshipSystem.Core.Entities;
using DNC.InternshipSystem.Core.Enums;
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
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);
            if (lecturer == null) return View("Error");

            // Lay danh sach lop GV phu trach + dem so SV bang query rieng
            var myClasses = await _context.Classes
                .Where(c => c.LecturerId == lecturer.UserId)
                .Include(c => c.Major)
                .OrderBy(c => c.Id)
                .ToListAsync();

            // Dem so SV cho tung lop
            var classIds = myClasses.Select(c => c.Id).ToList();
            var studentCounts = await _context.Students
                .Where(s => s.ClassId != null && classIds.Contains(s.ClassId))
                .GroupBy(s => s.ClassId)
                .Select(g => new { ClassId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ClassId!, x => x.Count);

            ViewBag.MyClasses = myClasses;
            ViewBag.StudentCounts = studentCounts;

            return View();
        }

        // AJAX: Lay danh sach SV theo lop (co phan trang)
        [HttpGet]
        public async Task<IActionResult> GetStudentsByClass(string classId, int page = 1, int pageSize = 20)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);
            if (lecturer == null) return Unauthorized();

            var cls = await _context.Classes.FirstOrDefaultAsync(c => c.Id == classId && c.LecturerId == lecturer.UserId);
            if (cls == null) return Forbid();

            var query = _context.Students
                .Include(s => s.User)
                .Where(s => s.ClassId == classId)
                .OrderBy(s => s.OrderNumber)
                .ThenBy(s => s.StudentCode);

            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var students = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Lay registration (neu co) cho tung SV
            var studentIds = students.Select(s => s.UserId).ToList();
            var registrations = await _context.Registrations
                .Include(r => r.Company)
                .Where(r => studentIds.Contains(r.StudentId))
                .GroupBy(r => r.StudentId)
                .Select(g => g.OrderByDescending(r => r.CreatedDate).First())
                .ToListAsync();

            ViewBag.Registrations = registrations.ToDictionary(r => r.StudentId);
            ViewBag.ClassId = classId;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;
            ViewBag.StartIndex = (page - 1) * pageSize;

            return PartialView("_MyStudentListPartial", students);
        }

        // GET: /Lecturer/MyStudents/Details/{studentId}
        public async Task<IActionResult> Details(Guid id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });
            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);

            var student = await _context.Students
                .Include(s => s.User)
                .Include(s => s.Class)
                    .ThenInclude(c => c!.Major)
                .FirstOrDefaultAsync(s => s.UserId == id &&
                    s.Class != null && s.Class.LecturerId == lecturer!.UserId);

            if (student == null) return NotFound();

            var registration = await _context.Registrations
                .Include(r => r.Company)
                .Include(r => r.Term)
                .Where(r => r.StudentId == id)
                .OrderByDescending(r => r.CreatedDate)
                .FirstOrDefaultAsync();

            Grade? grade = null;
            if (registration != null)
            {
                grade = await _context.Grades
                    .FirstOrDefaultAsync(g => g.RegistrationId == registration.Id);
            }

            var logbooks = registration != null
                ? await _context.Logbooks
                    .Where(l => l.RegistrationId == registration.Id)
                    .OrderBy(l => l.WeekNumber)
                    .ToListAsync()
                : new List<Logbook>();

            ViewBag.Student = student;
            ViewBag.Registration = registration;
            ViewBag.Grade = grade;
            ViewBag.Logbooks = logbooks;

            return View();
        }
    }
}
