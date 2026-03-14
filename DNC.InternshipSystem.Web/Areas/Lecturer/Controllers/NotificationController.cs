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
    public class NotificationController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public NotificationController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new List<object>());

            var lecturer = await _context.Lecturers
                .FirstOrDefaultAsync(l => l.UserId == user.Id);

            if (lecturer == null) return Json(new List<object>());

            var notifications = new List<object>();
            var now = DateTime.Now;

            // So SV duoc phan cong
            var assignedCount = await _context.Registrations
                .CountAsync(r => r.LecturerId == lecturer.UserId && r.Status == RegistrationStatus.Approved);

            if (assignedCount > 0)
            {
                notifications.Add(new
                {
                    icon = "fa-users",
                    color = "primary",
                    title = $"Bạn đang hướng dẫn {assignedCount} sinh viên",
                    message = "Kiểm tra nhật ký và tiến độ sinh viên.",
                    time = "",
                    important = false
                });
            }

            // Logbook chua xem (chua co comment)
            var uncommentedLogs = await _context.Logbooks
                .Include(l => l.Registration)
                .Where(l => l.Registration!.LecturerId == lecturer.UserId
                         && l.Registration.Status == RegistrationStatus.Approved
                         && l.LecturerComment == null)
                .CountAsync();

            if (uncommentedLogs > 0)
            {
                notifications.Add(new
                {
                    icon = "fa-book-open",
                    color = "warning",
                    title = $"{uncommentedLogs} nhật ký chưa nhận xét",
                    message = "Sinh viên đã ghi nhật ký, cần nhận xét từ bạn.",
                    time = "",
                    important = true
                });
            }

            // Bao cao chua duyet
            var pendingReports = await _context.Submissions
                .Include(s => s.Registration)
                .Where(s => s.Registration!.LecturerId == lecturer.UserId && s.Status == "Pending")
                .CountAsync();

            if (pendingReports > 0)
            {
                notifications.Add(new
                {
                    icon = "fa-file-alt",
                    color = "info",
                    title = $"{pendingReports} báo cáo chờ xem",
                    message = "Sinh viên đã nộp báo cáo thực tập.",
                    time = "",
                    important = true
                });
            }

            // Dot thuc tap
            var activeTerm = await _context.InternshipTerms
                .Where(t => t.IsActive)
                .OrderByDescending(t => t.Id)
                .FirstOrDefaultAsync();

            if (activeTerm != null && activeTerm.ReportDeadline > now && (activeTerm.ReportDeadline - now).TotalDays <= 7)
            {
                notifications.Add(new
                {
                    icon = "fa-calendar-times",
                    color = "danger",
                    title = "Sắp đến hạn nộp báo cáo",
                    message = $"Hạn nộp: {activeTerm.ReportDeadline:dd/MM/yyyy}. Nhắc sinh viên hoàn thành.",
                    time = "Khẩn cấp",
                    important = true
                });
            }

            return Json(notifications);
        }
    }
}
