using DNC.InternshipSystem.Core.Entities;
using DNC.InternshipSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DNC.InternshipSystem.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Lecturer")]
    public class GradingController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public GradingController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ==============================
        // HÀM TÍNH ĐIỂM 
        // ==============================
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

        // ==============================
        // INDEX
        // ==============================
        public async Task<IActionResult> Index(int? majorId = null, string? search = null)
        {
            var user = await _userManager.GetUserAsync(User);
            var isLecturer = User.IsInRole("Lecturer");

            ViewBag.CurrentMajorId = majorId;
            ViewBag.CurrentSearch = search;

            ViewBag.Majors = await _context.Majors
                .GroupBy(m => m.Name)
                .Select(g => g.First())
                .ToListAsync();

            var query = _context.Registrations
                .Include(r => r.Student).ThenInclude(s => s!.User)
                .Include(r => r.Student).ThenInclude(s => s!.Class).ThenInclude(c => c!.Major)
                .Include(r => r.Company)
                .Include(r => r.Lecturer).ThenInclude(l => l!.User)
                .Where(r => r.Status == 1 && r.LecturerId != null);

            if (isLecturer && user != null)
            {
                query = query.Where(r => r.LecturerId == user.Id);
            }

            if (majorId.HasValue)
            {
                var selectedMajor = await _context.Majors.FindAsync(majorId);
                if (selectedMajor != null)
                {
                    query = query.Where(r => r.Student!.Class!.Major!.Name == selectedMajor.Name);
                }
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(r =>
                    r.Student!.User!.FullName.Contains(search) ||
                    r.Student.StudentCode.Contains(search));
            }

            var students = await query.ToListAsync();

            // Tính lại điểm để hiển thị
            foreach (var s in students)
            {
                s.FinalScore = CalculateFinalScore(s.CompanyScore, s.InstructorScore);
            }

            return View(students);
        }

        // ==============================
        // UPDATE SCORES
        // ==============================
        [HttpPost]
        public async Task<IActionResult> UpdateScores(Guid id, double? companyScore, double? instructorScore)
        {
            var registration = await _context.Registrations.FindAsync(id);
            if (registration == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sinh viên!" });
            }

            if (User.IsInRole("Lecturer"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (registration.LecturerId.ToString() != userId)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Bạn không được phân công hướng dẫn sinh viên này!"
                    });
                }
            }

            registration.CompanyScore = companyScore;
            registration.InstructorScore = instructorScore;

            registration.FinalScore = CalculateFinalScore(companyScore, instructorScore);

            registration.UpdatedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                finalScore = registration.FinalScore
            });
        }

        // ==============================
        // EXPORT EXCEL
        // ==============================
        public async Task<IActionResult> Export(int? majorId = null, string? search = null)
        {
            var user = await _userManager.GetUserAsync(User);
            var isLecturer = User.IsInRole("Lecturer");

            var query = _context.Registrations
                .Include(r => r.Student).ThenInclude(s => s!.User)
                .Include(r => r.Student)
                    .ThenInclude(s => s!.Class)
                    .ThenInclude(c => c!.Major)
                    .ThenInclude(m => m!.Batch)
                .Include(r => r.Company)
                .Include(r => r.Lecturer).ThenInclude(l => l!.User)
                .Where(r => r.Status == 1 && r.LecturerId != null);

            if (isLecturer && user != null)
            {
                query = query.Where(r => r.LecturerId == user.Id);
            }

            if (majorId.HasValue)
            {
                var selectedMajor = await _context.Majors.FindAsync(majorId);
                if (selectedMajor != null)
                {
                    query = query.Where(r => r.Student!.Class!.Major!.Name == selectedMajor.Name);
                }
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(r =>
                    r.Student!.User!.FullName.Contains(search) ||
                    r.Student.StudentCode.Contains(search));
            }

            var students = await query.ToListAsync();

            using (var package = new OfficeOpenXml.ExcelPackage())
            {
                var workSheet = package.Workbook.Worksheets.Add("KetQuaThucTap");

                workSheet.Cells[1, 1].Value = "STT";
                workSheet.Cells[1, 2].Value = "MSSV";
                workSheet.Cells[1, 3].Value = "Họ tên";
                workSheet.Cells[1, 4].Value = "Lớp";
                workSheet.Cells[1, 5].Value = "Ngành";
                workSheet.Cells[1, 6].Value = "Công ty thực tập";
                workSheet.Cells[1, 7].Value = "GVHD";
                workSheet.Cells[1, 8].Value = "Điểm DN (40%)";
                workSheet.Cells[1, 9].Value = "Điểm GVHD (60%)";
                workSheet.Cells[1, 10].Value = "Tổng kết";
                workSheet.Cells[1, 11].Value = "Xếp loại";

                int recordIndex = 2;
                int stt = 1;

                foreach (var s in students)
                {
                    var finalScore = CalculateFinalScore(s.CompanyScore, s.InstructorScore);

                    workSheet.Cells[recordIndex, 1].Value = stt++;
                    workSheet.Cells[recordIndex, 2].Value = s.Student?.StudentCode;
                    workSheet.Cells[recordIndex, 3].Value = s.Student?.User?.FullName;
                    workSheet.Cells[recordIndex, 4].Value = s.Student?.Class?.Name;
                    workSheet.Cells[recordIndex, 5].Value = s.Student?.Class?.Major?.Name;
                    workSheet.Cells[recordIndex, 6].Value = s.Company?.Name;
                    workSheet.Cells[recordIndex, 7].Value = s.Lecturer?.User?.FullName;
                    workSheet.Cells[recordIndex, 8].Value = s.CompanyScore;
                    workSheet.Cells[recordIndex, 9].Value = s.InstructorScore;
                    workSheet.Cells[recordIndex, 10].Value = finalScore;

                    string status = "";
                    if (finalScore.HasValue)
                    {
                        status = finalScore >= 5.0 ? "Đạt" : "Không đạt";
                    }

                    workSheet.Cells[recordIndex, 11].Value = status;

                    recordIndex++;
                }

                workSheet.Cells.AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                string excelName = $"KetQuaThucTap-{DateTime.Now:yyyyMMddHHmmss}.xlsx";

                return File(stream,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    excelName);
            }
        }
    }
}