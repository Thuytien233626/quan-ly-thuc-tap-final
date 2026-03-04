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

        /// <summary>
        /// Tính điểm tổng = (Điểm DN * 0.4) + (Điểm GVHD * 0.6)
        /// </summary>
        /// <remarks>
        /// Công thức: (CompanyScore * 0.4) + (InstructorScore * 0.6)
        /// Trả về null nếu thiếu bất kỳ điểm nào
        /// </remarks>
        private double? CalculateFinalScore(double? companyScore, double? instructorScore)
        {
            if (companyScore.HasValue && instructorScore.HasValue)
            {
                return Math.Round(
                    (companyScore.Value * 0.4) + 
                    (instructorScore.Value * 0.6), 2);
            }

            return null;
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
            // Xác thực người dùng
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account", new { area = "" });

            // Lấy thông tin giảng viên
            var lecturer = await _context.Lecturers
                .FirstOrDefaultAsync(l => l.UserId == user.Id);

            if (lecturer == null)
                return Forbid();

            // Lấy thông tin đăng ký thực tập
            var registration = await _context.Registrations
                .FirstOrDefaultAsync(r => r.Id == gradeModel.RegistrationId);

            if (registration == null || registration.LecturerId != lecturer.UserId)
                return Forbid();

            // Lấy hoặc tạo bản ghi điểm chấm công
            var existingGrade = await _context.Grades
                .FirstOrDefaultAsync(g => g.RegistrationId == gradeModel.RegistrationId);

            if (existingGrade == null)
            {
                // Tạo bản ghi mới
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
                // Cập nhật bản ghi hiện tại
                existingGrade.InstructorScore = gradeModel.InstructorScore;
                existingGrade.Note = gradeModel.Note;
                existingGrade.GradedDate = DateTime.Now;
            }

            // **FIX: Cập nhật điểm tổng trên Registration**
            registration.InstructorScore = gradeModel.InstructorScore;
            registration.FinalScore = CalculateFinalScore(registration.CompanyScore, gradeModel.InstructorScore);
            registration.UpdatedDate = DateTime.Now;  // ← QUAN TRỌNG: Ghi dấu thời gian cập nhật

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã lưu điểm thành công.";
            return RedirectToAction("Index");
        }
    }
}
