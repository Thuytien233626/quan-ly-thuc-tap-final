using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DNC.InternshipSystem.Core.Entities;
using DNC.InternshipSystem.Core.Enums;
using DNC.InternshipSystem.Infrastructure.Data;

namespace DNC.InternshipSystem.Web.Areas.Student.Controllers
{
    [Area("Student")]
    [Authorize(Roles = "Student")]
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

            var student = await _context.Students
                .Include(s => s.Class)
                    .ThenInclude(c => c!.Lecturer)
                        .ThenInclude(l => l.User)
                .FirstOrDefaultAsync(s => s.UserId == user.Id);

            if (student == null) return Json(new List<object>());

            var notifications = new List<object>();
            var now = DateTime.Now;

            // Lay dot thuc tap hien tai
            var currentTerm = await _context.InternshipTerms
                .Include(t => t.Batch)
                .Where(t => t.IsActive)
                .OrderByDescending(t => t.Id)
                .FirstOrDefaultAsync();

            if (currentTerm != null)
            {
                // Thong bao dot thuc tap dang mo
                if (now >= currentTerm.RegistrationStart && now <= currentTerm.RegistrationEnd)
                {
                    var daysLeft = (currentTerm.RegistrationEnd - now).Days;
                    notifications.Add(new
                    {
                        icon = "fa-door-open",
                        color = "success",
                        title = "Đợt đăng ký thực tập đang mở!",
                        message = $"HK{currentTerm.Semester} - {currentTerm.AcademicYear}. Còn {daysLeft} ngày để đăng ký.",
                        time = currentTerm.RegistrationStart.ToString("dd/MM/yyyy"),
                        important = true
                    });
                }

                // Thong bao sap het han dang ky (3 ngay)
                if (now < currentTerm.RegistrationEnd && (currentTerm.RegistrationEnd - now).TotalDays <= 3)
                {
                    notifications.Add(new
                    {
                        icon = "fa-exclamation-triangle",
                        color = "warning",
                        title = "Sắp hết hạn đăng ký!",
                        message = $"Hạn cuối: {currentTerm.RegistrationEnd:dd/MM/yyyy}. Hãy hoàn tất đăng ký ngay.",
                        time = "Khẩn cấp",
                        important = true
                    });
                }

                // Thong bao bat dau thuc tap
                if (now >= currentTerm.InternshipStart && now <= currentTerm.InternshipStart.AddDays(7))
                {
                    notifications.Add(new
                    {
                        icon = "fa-play-circle",
                        color = "info",
                        title = "Đợt thực tập đã bắt đầu!",
                        message = $"Thời gian bắt đầu: {currentTerm.InternshipStart:dd/MM/yyyy}. Hãy liên hệ doanh nghiệp.",
                        time = currentTerm.InternshipStart.ToString("dd/MM/yyyy"),
                        important = false
                    });
                }

                // Thong bao sap den han nop bao cao (7 ngay)
                if (currentTerm.ReportDeadline > now && (currentTerm.ReportDeadline - now).TotalDays <= 7)
                {
                    var daysLeft = (currentTerm.ReportDeadline - now).Days;
                    notifications.Add(new
                    {
                        icon = "fa-file-upload",
                        color = "danger",
                        title = "Sắp đến hạn nộp báo cáo!",
                        message = $"Hạn nộp: {currentTerm.ReportDeadline:dd/MM/yyyy}. Còn {daysLeft} ngày.",
                        time = "Khẩn cấp",
                        important = true
                    });
                }
            }

            // Thong bao GV huong dan
            if (student.Class?.Lecturer?.User != null)
            {
                var lecName = student.Class.Lecturer.User.FullName;
                var rank = !string.IsNullOrEmpty(student.Class.Lecturer.AcademicRank) 
                    ? student.Class.Lecturer.AcademicRank + ". " : "";
                notifications.Add(new
                {
                    icon = "fa-chalkboard-teacher",
                    color = "primary",
                    title = "Giảng viên hướng dẫn đã được phân công",
                    message = $"{rank}{lecName} phụ trách lớp {student.ClassId}.",
                    time = "",
                    important = false
                });
            }

            // Kiem tra don dang ky
            var registration = await _context.Registrations
                .Where(r => r.StudentId == student.UserId)
                .OrderByDescending(r => r.CreatedDate)
                .FirstOrDefaultAsync();

            if (registration != null)
            {
                switch (registration.Status)
                {
                    case RegistrationStatus.Approved:
                        notifications.Add(new
                        {
                            icon = "fa-check-circle",
                            color = "success",
                            title = "Đơn đăng ký đã được duyệt!",
                            message = "Đơn thực tập của bạn đã được admin phê duyệt.",
                            time = registration.UpdatedDate?.ToString("dd/MM/yyyy") ?? "",
                            important = true
                        });
                        break;
                    case RegistrationStatus.Rejected:
                        notifications.Add(new
                        {
                            icon = "fa-times-circle",
                            color = "danger",
                            title = "Đơn đăng ký bị từ chối",
                            message = "Vui lòng liên hệ admin để biết thêm chi tiết.",
                            time = registration.UpdatedDate?.ToString("dd/MM/yyyy") ?? "",
                            important = true
                        });
                        break;
                }
            }

            return Json(notifications);
        }
    }
}
