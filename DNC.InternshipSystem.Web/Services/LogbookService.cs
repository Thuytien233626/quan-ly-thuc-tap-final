using DNC.InternshipSystem.Core.Entities;
using DNC.InternshipSystem.Core.Interfaces;
using DNC.InternshipSystem.Core.Models;
using DNC.InternshipSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DNC.InternshipSystem.Web.Services
{
    public class LogbookService : ILogbookService
    {
        private readonly AppDbContext _context;
        private readonly AIMentorService _aiService;
        private readonly ILogger<LogbookService> _logger;

        public LogbookService(AppDbContext context, AIMentorService aiService, ILogger<LogbookService> logger)
        {
            _context = context;
            _aiService = aiService;
            _logger = logger;
        }

        public async Task<Registration?> GetActiveRegistration(Guid userId)
        {
            return await _context.Registrations.Include(r => r.Term).Where(r => r.StudentId == userId).OrderByDescending(r => r.CreatedDate).FirstOrDefaultAsync();
        }

        public async Task<ServiceResult> CreateLogbook(Guid userId, int weekNumber, DateTime startDate, DateTime endDate, string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return ServiceResult.Fail("Nội dung nhật ký không được để trống.");
            if (weekNumber < 1 || weekNumber > 20) return ServiceResult.Fail("Số tuần phải từ 1 đến 20.");

            var registration = await _context.Registrations.Where(r => r.StudentId == userId).OrderByDescending(r => r.CreatedDate).FirstOrDefaultAsync();
            if (registration == null) return ServiceResult.Fail("Bạn chưa có đơn đăng ký thực tập.");

            var existed = await _context.Logbooks.AnyAsync(l => l.RegistrationId == registration.Id && l.WeekNumber == weekNumber);
            if (existed) return ServiceResult.Fail("Tuần này đã có nhật ký.");

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

            try
            {
                var aiResult = await _aiService.AnalyzeLogbook(content);
                logbook.AISummary = aiResult.AISummary;
                logbook.AiScore = aiResult.AiScore;
                logbook.AIWarning = aiResult.AIWarning;
                logbook.AIWarningDetails = aiResult.AIWarningDetails;
                logbook.AiSuggestions = aiResult.AiSuggestions;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loi khi phan tich logbook voi AI");
                logbook.AISummary = "AI Mentor: Phân tích không khả dụng lúc này.";
                logbook.AiScore = 0;
                await _context.SaveChangesAsync();
            }

            return ServiceResult.Ok("Lưu nhật ký thành công!");
        }

        public async Task<List<Logbook>> GetLogbooks(Guid registrationId)
        {
            return await _context.Logbooks.Where(l => l.RegistrationId == registrationId).OrderBy(l => l.WeekNumber).ToListAsync();
        }

        public async Task<ServiceResult> AddComment(Guid logbookId, Guid lecturerId, string comment)
        {
            var logbook = await _context.Logbooks.FindAsync(logbookId);
            if (logbook == null) return ServiceResult.Fail("Không tìm thấy nhật ký.");

            var registration = await _context.Registrations.FindAsync(logbook.RegistrationId);
            if (registration?.LecturerId != lecturerId) return ServiceResult.Fail("Bạn không có quyền nhận xét nhật ký này.");

            logbook.LecturerComment = comment;
            await _context.SaveChangesAsync();
            return ServiceResult.Ok("Đã lưu nhận xét.");
        }

        public async Task<ServiceResult<string>> AnalyzeWithAI(Guid logbookId, Guid lecturerId)
        {
            var logbook = await _context.Logbooks.Include(l => l.Registration).FirstOrDefaultAsync(l => l.Id == logbookId);
            if (logbook == null) return ServiceResult<string>.Fail("Logbook không tồn tại.");
            if (logbook.Registration?.LecturerId != lecturerId) return ServiceResult<string>.Fail("Bạn không có quyền truy cập logbook này.");

            var aiResult = await _aiService.AnalyzeLogbook(logbook.Content);

            logbook.AISummary = aiResult.AISummary;
            logbook.AiScore = aiResult.AiScore;
            logbook.AIWarning = aiResult.AIWarning;
            logbook.AIWarningDetails = aiResult.AIWarningDetails;
            logbook.AiSuggestions = aiResult.AiSuggestions;
            await _context.SaveChangesAsync();

            return ServiceResult<string>.Ok(aiResult.AISummary ?? string.Empty, "Đã phân tích logbook với AI Mentor.");
        }

        public async Task<ServiceResult> UpdateLogbook(Guid logbookId, Guid userId, string content, string? evidenceUrl)
        {
            if (string.IsNullOrWhiteSpace(content)) return ServiceResult.Fail("Nội dung nhật ký không được để trống.");

            var logbook = await _context.Logbooks.Include(l => l.Registration).FirstOrDefaultAsync(l => l.Id == logbookId);
            if (logbook == null) return ServiceResult.Fail("Không tìm thấy nhật ký.");
            if (logbook.Registration?.StudentId != userId) return ServiceResult.Fail("Bạn không có quyền sửa nhật ký này.");
            if (!string.IsNullOrEmpty(logbook.LecturerComment)) return ServiceResult.Fail("Không thể sửa nhật ký đã được giảng viên nhận xét.");

            logbook.Content = content;
            logbook.EvidenceImageUrl = evidenceUrl;
            logbook.SubmittedDate = DateTime.UtcNow;

            try
            {
                var aiResult = await _aiService.AnalyzeLogbook(content);
                logbook.AISummary = aiResult.AISummary;
                logbook.AiScore = aiResult.AiScore;
                logbook.AIWarning = aiResult.AIWarning;
                logbook.AIWarningDetails = aiResult.AIWarningDetails;
                logbook.AiSuggestions = aiResult.AiSuggestions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Loi AI khi cap nhat logbook {LbId}", logbookId);
            }

            await _context.SaveChangesAsync();
            return ServiceResult.Ok("Cập nhật nhật ký thành công!");
        }

        public async Task<ServiceResult> DeleteLogbook(Guid logbookId, Guid userId)
        {
            var logbook = await _context.Logbooks.Include(l => l.Registration).FirstOrDefaultAsync(l => l.Id == logbookId);
            if (logbook == null) return ServiceResult.Fail("Không tìm thấy nhật ký.");
            if (logbook.Registration?.StudentId != userId) return ServiceResult.Fail("Bạn không có quyền xóa nhật ký này.");
            if (!string.IsNullOrEmpty(logbook.LecturerComment)) return ServiceResult.Fail("Không thể xóa nhật ký đã được giảng viên nhận xét.");

            _context.Logbooks.Remove(logbook);
            await _context.SaveChangesAsync();
            return ServiceResult.Ok("Đã xóa nhật ký thành công!");
        }

        public async Task<ServiceResult> RequestResubmit(Guid logbookId, Guid lecturerId, string reason)
        {
            var logbook = await _context.Logbooks.Include(l => l.Registration).FirstOrDefaultAsync(l => l.Id == logbookId);
            if (logbook == null) return ServiceResult.Fail("Không tìm thấy nhật ký.");
            if (logbook.Registration?.LecturerId != lecturerId) return ServiceResult.Fail("Bạn không có quyền thao tác nhật ký này.");
            if (string.IsNullOrEmpty(logbook.LecturerComment)) return ServiceResult.Fail("Nhật ký này chưa được duyệt.");

            logbook.LecturerComment = null;
            logbook.AISummary = null;
            logbook.AiSuggestions = null;
            logbook.AiScore = null;
            logbook.AIWarning = false;
            logbook.AIWarningDetails = null;
            logbook.ResubmitRequested = false;
            logbook.ResubmitReason = null;
            await _context.SaveChangesAsync();

            return ServiceResult.Ok("Đã yêu cầu sinh viên nộp lại nhật ký.");
        }

        public async Task<ServiceResult> StudentRequestResubmit(Guid logbookId, Guid userId, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason)) return ServiceResult.Fail("Vui lòng nhập lý do xin nộp lại.");
            var logbook = await _context.Logbooks.Include(l => l.Registration).FirstOrDefaultAsync(l => l.Id == logbookId);
            if (logbook == null) return ServiceResult.Fail("Không tìm thấy nhật ký.");
            if (logbook.Registration?.StudentId != userId) return ServiceResult.Fail("Bạn không có quyền thao tác nhật ký này.");
            if (string.IsNullOrEmpty(logbook.LecturerComment)) return ServiceResult.Fail("Nhật ký chưa được duyệt, bạn có thể sửa trực tiếp.");
            if (logbook.ResubmitRequested) return ServiceResult.Fail("Yêu cầu nộp lại đã được gửi trước đó.");

            logbook.ResubmitRequested = true;
            logbook.ResubmitReason = reason;
            await _context.SaveChangesAsync();
            return ServiceResult.Ok("Đã gửi yêu cầu nộp lại tới giảng viên.");
        }

        public async Task<ServiceResult> ApproveResubmitRequest(Guid logbookId, Guid lecturerId)
        {
            var logbook = await _context.Logbooks.Include(l => l.Registration).FirstOrDefaultAsync(l => l.Id == logbookId);
            if (logbook == null) return ServiceResult.Fail("Không tìm thấy nhật ký.");
            if (logbook.Registration?.LecturerId != lecturerId) return ServiceResult.Fail("Bạn không có quyền thao tác nhật ký này.");

            logbook.LecturerComment = null;
            logbook.AISummary = null;
            logbook.AiSuggestions = null;
            logbook.AiScore = null;
            logbook.AIWarning = false;
            logbook.AIWarningDetails = null;
            logbook.ResubmitRequested = false;
            logbook.ResubmitReason = null;
            await _context.SaveChangesAsync();
            return ServiceResult.Ok("Đã chấp nhận, sinh viên có thể nộp lại.");
        }

        public async Task<ServiceResult> RejectResubmitRequest(Guid logbookId, Guid lecturerId)
        {
            var logbook = await _context.Logbooks.Include(l => l.Registration).FirstOrDefaultAsync(l => l.Id == logbookId);
            if (logbook == null) return ServiceResult.Fail("Không tìm thấy nhật ký.");
            if (logbook.Registration?.LecturerId != lecturerId) return ServiceResult.Fail("Bạn không có quyền thao tác nhật ký này.");

            logbook.ResubmitRequested = false;
            logbook.ResubmitReason = null;
            await _context.SaveChangesAsync();
            return ServiceResult.Ok("Đã từ chối yêu cầu nộp lại.");
        }
    }
}