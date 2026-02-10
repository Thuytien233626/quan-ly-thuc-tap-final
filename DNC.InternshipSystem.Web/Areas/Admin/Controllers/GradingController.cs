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
    [Authorize(Roles = "Admin,Lecturer")] // Cho phep ca Admin va Giang vien
    public class GradingController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public GradingController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int? majorId = null, string? search = null)
        {
            var user = await _userManager.GetUserAsync(User);
            var isLecturer = User.IsInRole("Lecturer");
            
            // Tieu de trang


            ViewBag.CurrentMajorId = majorId;
            ViewBag.CurrentSearch = search;

            // Lay danh sach nganh de loc
            ViewBag.Majors = await _context.Majors
                .GroupBy(m => m.Name)
                .Select(g => g.First())
                .ToListAsync();

            var query = _context.Registrations
                .Include(r => r.Student).ThenInclude(s => s!.User)
                .Include(r => r.Student).ThenInclude(s => s!.Class).ThenInclude(c => c!.Major)
                .Include(r => r.Company)
                .Include(r => r.Lecturer).ThenInclude(l => l!.User)
                .Where(r => r.Status == 1 && r.LecturerId != null); // Da duyet & Da phan cong

            // Neu la Giang vien, chi lay sinh vien duoc phan cong cho ho
            if (isLecturer && user != null)
            {
                query = query.Where(r => r.LecturerId == user.Id);
            }

            // Loc theo ten nganh
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
                    (r.Student!.User!.FullName.Contains(search)) ||
                    (r.Student.StudentCode.Contains(search))
                );
            }

            var students = await query.ToListAsync();

            // Tinh diem tong ket de hien thi
            foreach (var s in students)
            {
                 if (s.CompanyScore.HasValue && s.ReportScore.HasValue && s.VivaScore.HasValue)
                 {
                     s.FinalScore = Math.Round((s.CompanyScore.Value * 0.4) + (s.ReportScore.Value * 0.3) + (s.VivaScore.Value * 0.3), 2);
                 }
            }

            return View(students);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateScores(Guid id, double? companyScore, double? reportScore, double? vivaScore)
        {
            var registration = await _context.Registrations.FindAsync(id);
            if (registration == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sinh viên!" });
            }

            // Kiem tra quyen: Neu la giang vien, phai duoc phan cong cho sinh vien nay
            if (User.IsInRole("Lecturer"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (registration.LecturerId.ToString() != userId)
                {
                    return Json(new { success = false, message = "Bạn không được phân công hướng dẫn sinh viên này!" });
                }
            }

            registration.CompanyScore = companyScore;
            registration.ReportScore = reportScore;
            registration.VivaScore = vivaScore;

            // Tinh diem tong ket
            if (companyScore.HasValue && reportScore.HasValue && vivaScore.HasValue)
            {
                registration.FinalScore = Math.Round((companyScore.Value * 0.4) + (reportScore.Value * 0.3) + (vivaScore.Value * 0.3), 2);
                
                if (registration.FinalScore >= 5.0)
                {
                    // Cap nhat trang thai neu can trong tuong lai
                }
            }
            else
            {
                registration.FinalScore = null;
            }

            registration.UpdatedDate = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new { success = true, finalScore = registration.FinalScore });
        }

        public async Task<IActionResult> Export(int? majorId = null, string? search = null)
        {
            var user = await _userManager.GetUserAsync(User);
            var isLecturer = User.IsInRole("Lecturer");

            var query = _context.Registrations
                .Include(r => r.Student).ThenInclude(s => s!.User)
                // Lay thong tin Sinh vien -> Lop -> Nganh -> Khoa hoc trong 1 chuoi include
                .Include(r => r.Student)
                    .ThenInclude(s => s!.Class)
                    .ThenInclude(c => c!.Major)
                    .ThenInclude(m => m!.Batch)
                .Include(r => r.Company)
                .Include(r => r.Lecturer).ThenInclude(l => l!.User)
                .Where(r => r.Status == 1 && r.LecturerId != null);

            // Loc theo Giang vien
            if (isLecturer && user != null)
            {
                var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.UserId == user.Id);
                if (lecturer != null)
                {
                    query = query.Where(r => r.LecturerId == lecturer.UserId);
                }
            }

            // LOC: Ten Nganh (su dung cung logic voi Index)
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
                    (r.Student!.User!.FullName.Contains(search)) ||
                    (r.Student.StudentCode.Contains(search))
                );
            }

            var students = await query.ToListAsync();

            // KHOI TAO FILE EXCEL
            using (var package = new OfficeOpenXml.ExcelPackage())
            {
                var workSheet = package.Workbook.Worksheets.Add("KetQuaThucTap");

                // Tieu de cot
                workSheet.Cells[1, 1].Value = "STT";
                workSheet.Cells[1, 2].Value = "MSSV";
                workSheet.Cells[1, 3].Value = "Họ tên";
                workSheet.Cells[1, 4].Value = "Lớp";
                workSheet.Cells[1, 5].Value = "Ngành";
                workSheet.Cells[1, 6].Value = "Công ty thực tập";
                workSheet.Cells[1, 7].Value = "GVHD";
                workSheet.Cells[1, 8].Value = "Điểm DN (40%)";
                workSheet.Cells[1, 9].Value = "Điểm BC (30%)";
                workSheet.Cells[1, 10].Value = "Điểm VĐ (30%)";
                workSheet.Cells[1, 11].Value = "Tổng kết";
                workSheet.Cells[1, 12].Value = "Xếp loại";

                // Du lieu
                int recordIndex = 2;
                int stt = 1;
                foreach (var s in students)
                {
                    // Tinh lai diem tong ket de dam bao chinh xac
                    double? finalScore = s.FinalScore;
                    if (!finalScore.HasValue && s.CompanyScore.HasValue && s.ReportScore.HasValue && s.VivaScore.HasValue)
                    {
                         finalScore = Math.Round((s.CompanyScore.Value * 0.4) + (s.ReportScore.Value * 0.3) + (s.VivaScore.Value * 0.3), 2);
                    }

                    workSheet.Cells[recordIndex, 1].Value = stt++;
                    workSheet.Cells[recordIndex, 2].Value = s.Student?.StudentCode;
                    workSheet.Cells[recordIndex, 3].Value = s.Student?.User?.FullName;
                    workSheet.Cells[recordIndex, 4].Value = s.Student?.Class?.Name;
                    workSheet.Cells[recordIndex, 5].Value = s.Student?.Class?.Major?.Name;
                    workSheet.Cells[recordIndex, 6].Value = s.Company?.Name;
                    workSheet.Cells[recordIndex, 7].Value = s.Lecturer?.User?.FullName;
                    
                    workSheet.Cells[recordIndex, 8].Value = s.CompanyScore;
                    workSheet.Cells[recordIndex, 9].Value = s.ReportScore;
                    workSheet.Cells[recordIndex, 10].Value = s.VivaScore;
                    workSheet.Cells[recordIndex, 11].Value = finalScore;

                    // Trang thai ket qua
                    string status = "";
                    if (finalScore.HasValue)
                    {
                        status = finalScore >= 5.0 ? "Đạt" : "Không đạt";
                    }
                    workSheet.Cells[recordIndex, 12].Value = status;

                    recordIndex++;
                }

                // Tu dong can chinh do rong cot
                workSheet.Cells.AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;
                string excelName = $"KetQuaThucTap-{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
            }
        }
    }
}
