using DNC.InternshipSystem.Core.Entities;
using DNC.InternshipSystem.Core.Interfaces;
using DNC.InternshipSystem.Core.Models;
using DNC.InternshipSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DNC.InternshipSystem.Web.Services
{
    /// <summary>
    /// Xu ly nghiep vu nhat ky thuc tap (Logbook)
    /// Kiem tra quyen truy cap + Tich hop AI phan tich
    /// </summary>
    public class LogbookService : ILogbookService
    {
        private readonly AppDbContext _context;
        private readonly AIMentorService _aiService;
        private readonly ILogger<LogbookService> _logger;

        public LogbookService(
            AppDbContext context,
            AIMentorService aiService,
            ILogger<LogbookService> logger)
        {
            _context = context;
            _aiService = aiService;
            _logger = logger;
        }

        public async Task<Registration?> GetActiveRegistration(Guid userId)
        {
            return await _context.Registrations
                .Include(r => r.Term)
                .Where(r => r.StudentId == userId)
                .OrderByDescending(r => r.CreatedDate)
                .FirstOrDefaultAsync();
        }

        public async Task<ServiceResult> CreateLogbook(Guid userId,
            int weekNumber, DateTime startDate, DateTime endDate, string content)
        {
            // Kiem tra noi dung khong duoc rong
            if (string.IsNullOrWhiteSpace(content))
                return ServiceResult.Fail("Nội dung nhật ký không được để trống.");

            // Kiem tra so tuan hop le
            if (weekNumber < 1 || weekNumber > 20)
                return ServiceResult.Fail("Số tuần phải từ 1 đến 20.");

            // Tim don dang ky cua sinh vien
            var registration = await _context.Registrations
                .Where(r => r.StudentId == userId)
                .OrderByDescending(r => r.CreatedDate)
                .FirstOrDefaultAsync();

            if (registration == null)
                return ServiceResult.Fail("Bạn chưa có đơn đăng ký thực tập.");

            // Kiem tra tuan da ton tai chua
            var existed = await _context.Logbooks
                .AnyAsync(l => l.RegistrationId == registration.Id && l.WeekNumber == weekNumber);

            if (existed)
                return ServiceResult.Fail("Tuần này đã có nhật ký.");

            // Tao logbook moi
            var logbook = new Logbook
            {
                RegistrationId = registration.Id,
                WeekNumber = weekNumber,
                StartDate = startDate,
                EndDate = endDate,
                Content = content,
                SubmittedDate = DateTime.UtcNow
            };

            _context.Logbooks.Add(logbook);
            await _context.SaveChangesAsync();

            // Goi AI phan tich (khong anh huong ket qua luu logbook)
            try
            {
                var aiResult = await _aiService.AnalyzeLogbook(content);
                logbook.AISummary = aiResult;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loi khi phan tich logbook voi AI, bo qua");
                logbook.AISummary = "AI Mentor: Phân tích không khả dụng lúc này.";
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("Sinh vien {UserId} da tao logbook tuan {Week}", userId, weekNumber);
            return ServiceResult.Ok("Lưu nhật ký thành công!");
        }

        public async Task<List<Logbook>> GetLogbooks(Guid registrationId)
        {
            return await _context.Logbooks
                .Where(l => l.RegistrationId == registrationId)
                .OrderBy(l => l.WeekNumber)
                .ToListAsync();
        }

        public async Task<ServiceResult> AddComment(Guid logbookId, Guid lecturerId, string comment)
        {
            var logbook = await _context.Logbooks.FindAsync(logbookId);
            if (logbook == null)
                return ServiceResult.Fail("Không tìm thấy nhật ký.");

            // Kiem tra quyen: GV phai la nguoi phu trach
            var registration = await _context.Registrations.FindAsync(logbook.RegistrationId);
            if (registration?.LecturerId != lecturerId)
                return ServiceResult.Fail("Bạn không có quyền nhận xét nhật ký này.");

            logbook.LecturerComment = comment;
            await _context.SaveChangesAsync();

            _logger.LogInformation("GV {LecId} da nhan xet logbook {LbId}", lecturerId, logbookId);
            return ServiceResult.Ok("Đã lưu nhận xét.");
        }

        public async Task<ServiceResult<string>> AnalyzeWithAI(Guid logbookId, Guid lecturerId)
        {
            var logbook = await _context.Logbooks
                .Include(l => l.Registration)
                .FirstOrDefaultAsync(l => l.Id == logbookId);

            if (logbook == null)
                return ServiceResult<string>.Fail("Logbook không tồn tại.");

            // Kiem tra quyen truy cap
            if (logbook.Registration?.LecturerId != lecturerId)
                return ServiceResult<string>.Fail("Bạn không có quyền truy cập logbook này.");

            _logger.LogInformation("Bat dau phan tich logbook {LbId} voi AI", logbookId);

            // Goi AI phan tich
            var aiFeedback = await _aiService.AnalyzeLogbook(logbook.Content);

            // Luu ket qua AI
            logbook.AISummary = aiFeedback;
            await _context.SaveChangesAsync();

            return ServiceResult<string>.Ok(aiFeedback, "Đã phân tích logbook với AI Mentor.");
        }
    }
}
