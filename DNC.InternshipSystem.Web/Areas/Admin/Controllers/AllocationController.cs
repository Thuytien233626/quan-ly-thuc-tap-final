using DNC.InternshipSystem.Core.Entities;
using DNC.InternshipSystem.Core.Enums;
using DNC.InternshipSystem.Core.Interfaces;
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
        private readonly IAllocationService _allocationService;

        public AllocationController(AppDbContext context, IAllocationService allocationService)
        {
            _context = context;
            _allocationService = allocationService;
        }

        // GET: /Admin/Allocation — Trang phan cong giang vien cho lop
        public async Task<IActionResult> Index(string? filter = null)
        {
            ViewData["Title"] = "Phân công Giảng viên";
            ViewBag.CurrentFilter = filter;

            // Lay toan bo lop hoc kem thong tin lien quan
            var classQuery = _context.Classes
                .Include(c => c.Major).ThenInclude(m => m!.Batch)
                .Include(c => c.Lecturer).ThenInclude(l => l!.User)
                .Include(c => c.Students)
                .AsQueryable();

            // Loc theo trang thai phan cong
            if (filter == "assigned")
                classQuery = classQuery.Where(c => c.LecturerId != null);
            else if (filter == "unassigned")
                classQuery = classQuery.Where(c => c.LecturerId == null);

            var classes = await classQuery
                .OrderBy(c => c.LecturerId == null ? 0 : 1)
                .ThenBy(c => c.Major != null ? c.Major.Name : "")
                .ThenBy(c => c.Name)
                .ToListAsync();

            // Dem so SV da dang ky va duoc duyet theo tung lop
            var approvedCounts = await _context.Registrations
                .Where(r => r.Status == RegistrationStatus.Approved)
                .Include(r => r.Student)
                .GroupBy(r => r.Student!.ClassId)
                .Select(g => new { ClassId = g.Key, Count = g.Count() })
                .ToListAsync();
            ViewBag.ApprovedCounts = approvedCounts.ToDictionary(x => x.ClassId ?? "", x => x.Count);

            // Lay danh sach giang vien de hien thi dropdown chon
            var lecturers = await _context.Lecturers
                .Include(l => l.User)
                .Include(l => l.Department)
                .Select(l => new LecturerOptionViewModel
                {
                    Id = l.UserId,
                    Code = l.LecturerCode,
                    Name = l.User!.FullName,
                    AcademicRank = l.AcademicRank ?? "",
                    DepartmentName = l.Department != null ? l.Department.Name : "",
                    ClassCount = _context.Classes.Count(c => c.LecturerId == l.UserId)
                })
                .OrderBy(l => l.Name)
                .ToListAsync();

            ViewBag.Lecturers = lecturers;

            // Thong ke tong quat
            int totalClasses = await _context.Classes.CountAsync();
            int assignedClasses = await _context.Classes.CountAsync(c => c.LecturerId != null);
            ViewBag.TotalClasses = totalClasses;
            ViewBag.AssignedClasses = assignedClasses;
            ViewBag.UnassignedClasses = totalClasses - assignedClasses;

            return View(classes);
        }

        // POST: Phan cong GV cho 1 lop (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignLecturerAjax(string classId, Guid? lecturerId)
        {
            var result = await _allocationService.AssignLecturer(classId, lecturerId);
            return Json(new { success = result.Success, lecturerName = result.Data, message = result.Message });
        }

        // POST: Phan cong GV cho nhieu lop cung luc (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkAssignAjax(string[] classIds, Guid lecturerId)
        {
            var result = await _allocationService.BulkAssign(classIds, lecturerId);
            return Json(new { success = result.Success, count = result.Data, message = result.Message });
        }

        // POST: Go phan cong GV khoi lop (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAssignmentAjax(string classId)
        {
            var result = await _allocationService.RemoveAssignment(classId);
            return Json(new { success = result.Success, message = result.Message });
        }
    }

    // ViewModel hien thi thong tin GV trong dropdown chon
    public class LecturerOptionViewModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string AcademicRank { get; set; } = "";
        public string DepartmentName { get; set; } = "";
        public int ClassCount { get; set; }
    }
}
