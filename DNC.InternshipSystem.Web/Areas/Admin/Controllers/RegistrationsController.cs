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
    public class RegistrationsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IRegistrationService _registrationService;

        public RegistrationsController(AppDbContext context, IRegistrationService registrationService)
        {
            _context = context;
            _registrationService = registrationService;
        }

        // GET: /Admin/Registrations — Danh sach don dang ky thuc tap
        public async Task<IActionResult> Index(int status = 0, string? search = null)
        {
            ViewData["Title"] = "Quản lý Đơn đăng ký";
            ViewBag.CurrentStatus = status;
            ViewBag.CurrentSearch = search;

            // Dem so luong don theo tung trang thai (hien thi tab)
            ViewBag.PendingCount = await _context.Registrations.CountAsync(r => r.Status == RegistrationStatus.Pending);
            ViewBag.ApprovedCount = await _context.Registrations.CountAsync(r => r.Status == RegistrationStatus.Approved);
            ViewBag.RejectedCount = await _context.Registrations.CountAsync(r => r.Status == RegistrationStatus.Rejected);
            ViewBag.CompletedCount = await _context.Registrations.CountAsync(r => r.Status == RegistrationStatus.Completed);

            var query = _context.Registrations
                .Include(r => r.Student).ThenInclude(s => s!.User)
                .Include(r => r.Student).ThenInclude(s => s!.Class).ThenInclude(c => c!.Major)
                .Include(r => r.Company)
                .Include(r => r.Term)
                .Include(r => r.Lecturer).ThenInclude(l => l!.User)
                .Where(r => r.Status == (RegistrationStatus)status);

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(r =>
                    (r.Student != null && r.Student.User != null && r.Student.User.FullName.Contains(search)) ||
                    (r.Student != null && r.Student.StudentCode.Contains(search)) ||
                    (r.Company != null && r.Company.Name.Contains(search)) ||
                    (r.ExternalCompanyName != null && r.ExternalCompanyName.Contains(search))
                );
            }

            var registrations = await query
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();

            return View(registrations);
        }

        // GET: Lay chi tiet don dang ky (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetDetailsAjax(Guid id)
        {
            var r = await _context.Registrations
                .Include(r => r.Student).ThenInclude(s => s!.User)
                .Include(r => r.Student).ThenInclude(s => s!.Class).ThenInclude(c => c!.Major).ThenInclude(m => m!.Batch)
                .Include(r => r.Company)
                .Include(r => r.Term)
                .Include(r => r.Lecturer).ThenInclude(l => l!.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (r == null) return Json(new { success = false });

            return Json(new
            {
                success = true,
                id = r.Id,
                studentCode = r.Student?.StudentCode ?? "",
                studentName = r.Student?.User?.FullName ?? "",
                studentEmail = r.Student?.User?.Email ?? "",
                studentPhone = r.Student?.Phone ?? "",
                className = r.Student?.Class?.Name ?? "",
                majorName = r.Student?.Class?.Major?.Name ?? "",
                batchName = r.Student?.Class?.Major?.Batch?.BatchCode ?? "",
                companyName = r.Company?.Name ?? r.ExternalCompanyName ?? "",
                companyAddress = r.Company?.Address ?? r.ExternalCompanyAddress ?? "",
                companyProvince = r.Company?.Province ?? "",
                companyContact = r.Company?.ContactPerson ?? "",
                companyPhone = r.Company?.PhoneNumber ?? "",
                isExternal = r.Company == null,
                position = r.Position,
                termName = r.Term?.TermName ?? "",
                lecturerName = r.Lecturer?.User?.FullName ?? "Chưa phân công",
                status = r.Status,
                rejectionReason = r.RejectionReason ?? "",
                createdDate = r.CreatedDate.ToString("dd/MM/yyyy HH:mm"),
                updatedDate = r.UpdatedDate?.ToString("dd/MM/yyyy HH:mm") ?? ""
            });
        }

        // POST: Duyet don dang ky (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveAjax(Guid id)
        {
            var result = await _registrationService.ApproveRegistration(id);
            return Json(new { success = result.Success, message = result.Message });
        }

        // POST: Tu choi don dang ky (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectAjax(Guid id, string reason)
        {
            var result = await _registrationService.RejectRegistration(id, reason);
            return Json(new { success = result.Success, message = result.Message });
        }

        // POST: Gan GV huong dan cho don cu the (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignLecturerAjax(Guid id, Guid lecturerId)
        {
            var result = await _registrationService.AssignLecturer(id, lecturerId);
            return Json(new { success = result.Success, message = result.Message });
        }

        // GET: Lay danh sach GV de chon (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetLecturersAjax()
        {
            var lecturers = await _context.Lecturers
                .Include(l => l.User)
                .Select(l => new
                {
                    id = l.UserId,
                    code = l.LecturerCode,
                    name = l.User != null ? l.User.FullName : ""
                })
                .ToListAsync();
            return Json(lecturers);
        }
    }
}
