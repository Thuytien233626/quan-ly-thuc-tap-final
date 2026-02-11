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
    public class GradingController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public GradingController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Lecturer/Grading
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });
            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);

            // Simple query - NO Include to avoid WITH/CTE SQL generation
            var registrations = await _context.Registrations
                .AsNoTracking()
                .Where(r => r.LecturerId == lecturer!.UserId && r.Status == 1)
                .ToListAsync();

            // Load related data separately with simple queries
            var studentIds = registrations.Select(r => r.StudentId).Distinct().ToList();
            var students = await _context.Students.AsNoTracking()
                .Where(s => studentIds.Contains(s.UserId)).ToListAsync();

            var userIds = students.Select(s => s.UserId).Distinct().ToList();
            var users = await _context.Users.AsNoTracking()
                .Where(u => userIds.Contains(u.Id)).ToListAsync();

            var classIds = students.Where(s => s.ClassId != null).Select(s => s.ClassId!).Distinct().ToList();
            var classes = await _context.Classes.AsNoTracking()
                .Where(c => classIds.Contains(c.Id)).ToListAsync();

            var companyIds = registrations.Where(r => r.CompanyId.HasValue).Select(r => r.CompanyId!.Value).Distinct().ToList();
            var companies = await _context.Companies.AsNoTracking()
                .Where(c => companyIds.Contains(c.Id)).ToListAsync();

            var regIds = registrations.Select(r => r.Id).ToList();
            var grades = await _context.Grades.AsNoTracking()
                .Where(g => regIds.Contains(g.RegistrationId)).ToListAsync();

            // Stitch data together in memory
            var studentDict = students.ToDictionary(s => s.UserId);
            var userDict = users.ToDictionary(u => u.Id);
            var classDict = classes.ToDictionary(c => c.Id);
            var companyDict = companies.ToDictionary(c => c.Id);

            foreach (var s in students)
            {
                if (userDict.TryGetValue(s.UserId, out var u)) s.User = u;
                if (s.ClassId != null && classDict.TryGetValue(s.ClassId, out var c)) s.Class = c;
            }
            foreach (var reg in registrations)
            {
                if (studentDict.TryGetValue(reg.StudentId, out var st)) reg.Student = st;
                if (reg.CompanyId.HasValue && companyDict.TryGetValue(reg.CompanyId.Value, out var co)) reg.Company = co;
            }

            ViewBag.Grades = grades;
            return View(registrations);
        }

        // GET: /Lecturer/Grading/Grade/{registrationId}
        public async Task<IActionResult> Grade(Guid id) // id la RegistrationId
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });
            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);

            var registration = await _context.Registrations
                .Include(r => r.Student).ThenInclude(s => s!.User)
                .Include(r => r.Student).ThenInclude(s => s!.Class)
                .FirstOrDefaultAsync(r => r.Id == id && r.LecturerId == lecturer!.UserId);

            if (registration == null) return NotFound();

            var grade = await _context.Grades.FirstOrDefaultAsync(g => g.RegistrationId == id);
            
            if (grade == null)
            {
                // Tao moi neu chua co
                grade = new Grade { RegistrationId = id };
            }

            ViewBag.Registration = registration;
            return View(grade);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Grade(Grade gradeModel)
        {
             var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });
            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);

            // Validate quyen so huu
            var registration = await _context.Registrations.FindAsync(gradeModel.RegistrationId);
            if (registration == null || registration.LecturerId != lecturer!.UserId)
            {
                return Forbid();
            }

            var existingGrade = await _context.Grades.FirstOrDefaultAsync(g => g.RegistrationId == gradeModel.RegistrationId);
            
            if (existingGrade == null)
            {
                existingGrade = new Grade
                {
                    RegistrationId = gradeModel.RegistrationId,
                    InstructorScore = gradeModel.InstructorScore,
                    Note = gradeModel.Note,
                    GradedDate = DateTime.Now
                };
                _context.Grades.Add(existingGrade);
            }
            else
            {
                existingGrade.InstructorScore = gradeModel.InstructorScore;
                existingGrade.Note = gradeModel.Note;
                existingGrade.GradedDate = DateTime.Now;
                _context.Grades.Update(existingGrade);
            }

            // Cap nhat luon vao bang Registration de dong bo (optional, tuy logic he thong)
            // registration.ReportScore = ... (neu can)
            // Hien tai Registration co cot FinalScore, se duoc tinh toan tu dong o noi khac hoac trigger
            
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã lưu điểm thành công.";
            return RedirectToAction("Index");
        }
    }
}
