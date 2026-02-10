using DNC.InternshipSystem.Core.Entities;
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

        public RegistrationsController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int status = 0, string? search = null)
        {
            ViewData["Title"] = "Quản lý Đơn đăng ký";
            ViewBag.CurrentStatus = status;
            ViewBag.CurrentSearch = search;

            // Get counts for tabs
            ViewBag.PendingCount = await _context.Registrations.CountAsync(r => r.Status == 0);
            ViewBag.ApprovedCount = await _context.Registrations.CountAsync(r => r.Status == 1);
            ViewBag.RejectedCount = await _context.Registrations.CountAsync(r => r.Status == 2);
            ViewBag.CompletedCount = await _context.Registrations.CountAsync(r => r.Status == 3);

            var query = _context.Registrations
                .Include(r => r.Student)
                    .ThenInclude(s => s!.User)
                .Include(r => r.Student)
                    .ThenInclude(s => s!.Class)
                        .ThenInclude(c => c!.Major)
                .Include(r => r.Company)
                .Include(r => r.Term)
                .Include(r => r.Lecturer)
                    .ThenInclude(l => l!.User)
                .Where(r => r.Status == status);

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

        [HttpGet]
        public async Task<IActionResult> GetDetailsAjax(Guid id)
        {
            var r = await _context.Registrations
                .Include(r => r.Student)
                    .ThenInclude(s => s!.User)
                .Include(r => r.Student)
                    .ThenInclude(s => s!.Class)
                        .ThenInclude(c => c!.Major)
                            .ThenInclude(m => m!.Batch)
                .Include(r => r.Company)
                .Include(r => r.Term)
                .Include(r => r.Lecturer)
                    .ThenInclude(l => l!.User)
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

        [HttpPost]
        public async Task<IActionResult> ApproveAjax(Guid id)
        {
            try
            {
                var registration = await _context.Registrations.FindAsync(id);
                if (registration == null) return Json(new { success = false, message = "Không tìm thấy đơn!" });

                registration.Status = 1; // Approved
                registration.UpdatedDate = DateTime.Now;
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RejectAjax(Guid id, string reason)
        {
            try
            {
                var registration = await _context.Registrations.FindAsync(id);
                if (registration == null) return Json(new { success = false, message = "Không tìm thấy đơn!" });

                registration.Status = 2; // Rejected
                registration.RejectionReason = reason;
                registration.UpdatedDate = DateTime.Now;
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AssignLecturerAjax(Guid id, Guid lecturerId)
        {
            try
            {
                var registration = await _context.Registrations.FindAsync(id);
                if (registration == null) return Json(new { success = false, message = "Không tìm thấy đơn!" });

                registration.LecturerId = lecturerId;
                registration.UpdatedDate = DateTime.Now;
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

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
