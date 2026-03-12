using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DNC.InternshipSystem.Core.Entities;
using DNC.InternshipSystem.Core.Enums;
using DNC.InternshipSystem.Core.Interfaces;
using DNC.InternshipSystem.Infrastructure.Data;

namespace DNC.InternshipSystem.Web.Areas.Lecturer.Controllers
{
    [Area("Lecturer")]
    [Authorize(Roles = "Lecturer")]
    public class GradingController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IGradingService _gradingService;

        public GradingController(
            AppDbContext context,
            UserManager<AppUser> userManager,
            IGradingService gradingService)
        {
            _context = context;
            _userManager = userManager;
            _gradingService = gradingService;
        }

        // GET: /Lecturer/Grading — Danh sach SV can cham diem
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });
            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);

            // Lay danh sach lop ma GV phu trach
            var lecturerClassIds = await _context.Classes
                .Where(c => c.LecturerId == lecturer!.UserId)
                .Select(c => c.Id)
                .ToListAsync();

            // Lay SV phan cong truc tiep va SV thuoc lop GV phu trach
            var allStudentIdsInClasses = lecturerClassIds.Any()
                ? await _context.Students.Where(s => lecturerClassIds.Contains(s.ClassId))
                    .Select(s => s.UserId).ToListAsync()
                : new List<Guid>();

            var registrations = await _context.Registrations
                .AsNoTracking()
                .Where(r => r.Status == RegistrationStatus.Approved &&
                    (r.LecturerId == lecturer!.UserId || allStudentIdsInClasses.Contains(r.StudentId)))
                .ToListAsync();

            // Tai du lieu lien quan rieng biet (tranh phuc tap SQL)
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

            // Ghep du lieu trong bo nho (thay vi dung Include phuc tap)
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

        // GET: /Lecturer/Grading/Grade/{id} — Form cham diem cho 1 SV
        public async Task<IActionResult> Grade(Guid id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });
            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);

            // Kiem tra quyen: GV phu trach truc tiep hoac qua lop
            var gradeClassIds = await _context.Classes
                .Where(c => c.LecturerId == lecturer!.UserId)
                .Select(c => c.Id)
                .ToListAsync();

            var registration = await _context.Registrations
                .Include(r => r.Student).ThenInclude(s => s!.User)
                .Include(r => r.Student).ThenInclude(s => s!.Class)
                .FirstOrDefaultAsync(r => r.Id == id &&
                    (r.LecturerId == lecturer!.UserId || gradeClassIds.Contains(r.Student!.ClassId)));

            if (registration == null) return NotFound();

            var grade = await _context.Grades.FirstOrDefaultAsync(g => g.RegistrationId == id);

            if (grade == null)
                grade = new Grade { RegistrationId = id };

            ViewBag.Registration = registration;
            return View(grade);
        }

        // POST: /Lecturer/Grading/Grade — Luu diem GV cham
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Grade(Grade gradeModel)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);
            if (lecturer == null) return Forbid();

            // Goi Service de luu diem (kiem tra quyen + dong bo Grade + Registration)
            var result = await _gradingService.UpdateScoresFromLecturer(
                gradeModel.RegistrationId,
                lecturer.UserId,
                gradeModel.InstructorScore,
                gradeModel.Note);

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.Message;
                return RedirectToAction("Grade", new { id = gradeModel.RegistrationId });
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction("Index");
        }
    }
}
