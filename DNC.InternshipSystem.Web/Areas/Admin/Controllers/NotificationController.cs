using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using DNC.InternshipSystem.Core.Enums;
using DNC.InternshipSystem.Infrastructure.Data;

namespace DNC.InternshipSystem.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class NotificationController : Controller
    {
        private readonly AppDbContext _context;

        public NotificationController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var notifications = new List<object>();
            var now = DateTime.Now;

            // Don dang ky cho duyet
            var pendingCount = await _context.Registrations
                .CountAsync(r => r.Status == RegistrationStatus.Pending);

            if (pendingCount > 0)
            {
                notifications.Add(new
                {
                    icon = "fa-file-alt",
                    color = "warning",
                    title = $"{pendingCount} đơn đăng ký chờ duyệt",
                    message = "Có đơn đăng ký thực tập cần được xét duyệt.",
                    time = "",
                    important = true
                });
            }

            // Bao cao cho duyet
            var pendingSubmissions = await _context.Submissions
                .CountAsync(s => s.Status == "Pending");

            if (pendingSubmissions > 0)
            {
                notifications.Add(new
                {
                    icon = "fa-file-upload",
                    color = "info",
                    title = $"{pendingSubmissions} báo cáo chờ duyệt",
                    message = "Sinh viên đã nộp báo cáo cần được kiểm tra.",
                    time = "",
                    important = true
                });
            }

            // Dot thuc tap dang hoat dong
            var activeTerm = await _context.InternshipTerms
                .Where(t => t.IsActive)
                .OrderByDescending(t => t.Id)
                .FirstOrDefaultAsync();

            if (activeTerm != null)
            {
                // Sap het han dang ky
                if (now < activeTerm.RegistrationEnd && (activeTerm.RegistrationEnd - now).TotalDays <= 3)
                {
                    notifications.Add(new
                    {
                        icon = "fa-exclamation-triangle",
                        color = "danger",
                        title = "Sắp hết hạn đăng ký!",
                        message = $"Hạn cuối: {activeTerm.RegistrationEnd:dd/MM/yyyy}. Còn {(activeTerm.RegistrationEnd - now).Days} ngày.",
                        time = "Khẩn cấp",
                        important = true
                    });
                }

                // Sap den han nop bao cao
                if (activeTerm.ReportDeadline > now && (activeTerm.ReportDeadline - now).TotalDays <= 7)
                {
                    notifications.Add(new
                    {
                        icon = "fa-calendar-times",
                        color = "warning",
                        title = "Sắp đến hạn nộp báo cáo",
                        message = $"Hạn nộp: {activeTerm.ReportDeadline:dd/MM/yyyy}.",
                        time = "",
                        important = false
                    });
                }
            }

            // SV chua phan cong GV
            var unassignedCount = await _context.Registrations
                .CountAsync(r => r.Status == RegistrationStatus.Approved && r.LecturerId == null);

            if (unassignedCount > 0)
            {
                notifications.Add(new
                {
                    icon = "fa-user-times",
                    color = "danger",
                    title = $"{unassignedCount} sinh viên chưa có GVHD",
                    message = "Cần phân công giảng viên hướng dẫn.",
                    time = "",
                    important = true
                });
            }

            return Json(notifications);
        }
    }
}
