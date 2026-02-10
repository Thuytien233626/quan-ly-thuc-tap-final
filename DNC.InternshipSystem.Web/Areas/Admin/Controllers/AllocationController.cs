using DNC.InternshipSystem.Core.Entities;
using DNC.InternshipSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DNC.InternshipSystem.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AllocationController : Controller
    {
        private readonly AppDbContext _context;

        public AllocationController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? majorId = null, string? search = null)
        {
            ViewData["Title"] = "Phân công Giảng viên";
            ViewBag.CurrentMajorId = majorId;
            ViewBag.CurrentSearch = search;

            // Lay danh sach nganh de loc
            ViewBag.Majors = await _context.Registrations
                .Where(r => r.Status == 1 && r.LecturerId == null)
                .Select(r => r.Student!.Class!.Major)
                .Where(m => m != null)
                .GroupBy(m => m!.Name)
                .Select(g => g!.First())
                .ToListAsync();

            // Get approved students awaiting assignment
            var studentQuery = _context.Registrations
                .Include(r => r.Student)
                    .ThenInclude(s => s!.User)
                 .Include(r => r.Student)
                    .ThenInclude(s => s!.Class)
                        .ThenInclude(c => c!.Major)
                .Include(r => r.Company)
                .Where(r => r.Status == 1 && r.LecturerId == null); // Trang thai 1 = Da duyet

            if (majorId.HasValue)
            {
                var selectedMajor = await _context.Majors.FindAsync(majorId);
                if (selectedMajor != null)
                {
                    studentQuery = studentQuery.Where(r => r.Student!.Class!.Major!.Name == selectedMajor.Name);
                }
            }

            if (!string.IsNullOrEmpty(search))
            {
                studentQuery = studentQuery.Where(r => 
                    (r.Student!.User!.FullName.Contains(search)) ||
                    (r.Student.StudentCode.Contains(search))
                );
            }

            var students = await studentQuery.ToListAsync();

            // 2. Lay danh sach giang vien va tai hien tai
            var lecturers = await _context.Lecturers
                .Include(l => l.User)
                // Tam thoi dem tat ca cac dang ky dang thuc tap duoc phan cong
                .Select(l => new LecturerAllocationViewModel
                {
                    Id = l.UserId,
                    Code = l.LecturerCode,
                    Name = l.User!.FullName,
                    DepartmentName = l.Department != null ? l.Department.Name : "",
                    CurrentLoad = _context.Registrations.Count(r => r.LecturerId == l.UserId && r.Status == 1) // Da duyet & Duoc phan cong
                })
                .ToListAsync();

            var viewModel = new AllocationViewModel
            {
                Students = students,
                Lecturers = lecturers
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> AssignStudents(List<Guid> studentIds, Guid lecturerId)
        {
            try
            {
                if (studentIds == null || !studentIds.Any())
                {
                    return Json(new { success = false, message = "Chưa chọn sinh viên nào!" });
                }

                var lecturer = await _context.Lecturers.FindAsync(lecturerId);
                if (lecturer == null)
                {
                    return Json(new { success = false, message = "Giảng viên không tồn tại!" });
                }

                int count = 0;
                foreach (var id in studentIds)
                {
                    await _context.Database.ExecuteSqlRawAsync(
                        "UPDATE Registrations SET LecturerId = {0}, UpdatedDate = {1} WHERE Id = {2}", 
                        lecturerId, DateTime.Now, id);
                    count++;
                }

                return Json(new { success = true, count = count, message = $"Đã phân công {count} sinh viên cho GV {lecturer.User?.FullName ?? lecturer.LecturerCode}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }

    // View Models (Luu tru noi bo hoac chuyen sang file rieng)
    public class AllocationViewModel
    {
        public List<Registration> Students { get; set; } = new();
        public List<LecturerAllocationViewModel> Lecturers { get; set; } = new();
    }

    public class LecturerAllocationViewModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public int CurrentLoad { get; set; }
    }
}
