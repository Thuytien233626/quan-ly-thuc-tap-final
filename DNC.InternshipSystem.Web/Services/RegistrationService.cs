using DNC.InternshipSystem.Core.Entities;
using DNC.InternshipSystem.Core.Enums;
using DNC.InternshipSystem.Core.Interfaces;
using DNC.InternshipSystem.Core.Models;
using DNC.InternshipSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DNC.InternshipSystem.Web.Services
{
    /// <summary>
    /// Xu ly toan bo nghiep vu dang ky thuc tap
    /// Bao gom kiem tra dieu kien dang ky, tao don, duyet/tu choi
    /// </summary>
    public class RegistrationService : IRegistrationService
    {
        private readonly AppDbContext _context;
        private readonly ICompanyService _companyService;
        private readonly ILogger<RegistrationService> _logger;

        public RegistrationService(
            AppDbContext context,
            ICompanyService companyService,
            ILogger<RegistrationService> logger)
        {
            _context = context;
            _companyService = companyService;
            _logger = logger;
        }

        /// <summary>
        /// Kiem tra dieu kien co ban truoc khi dang ky
        /// 1. Dot thuc tap phai dang mo (TermStatus == Open)
        /// 2. Con trong thoi han dang ky (RegistrationStart <= Now <= RegistrationEnd)
        /// 3. Sinh vien chua dang ky cho dot nay
        /// </summary>
        private async Task<(InternshipTerm? term, ServiceResult? error)> ValidateRegistration(Guid userId)
        {
            // Kiem tra dot thuc tap dang mo dang ky
            var activeTerm = await _context.InternshipTerms
                .Where(t => t.IsActive && t.Status == TermStatus.Open)
                .FirstOrDefaultAsync();

            if (activeTerm == null)
                return (null, ServiceResult.Fail("Hiện không có đợt thực tập nào đang mở đăng ký."));

            // Kiem tra thoi han dang ky
            var now = DateTime.UtcNow;
            if (now < activeTerm.RegistrationStart)
                return (null, ServiceResult.Fail($"Thời gian đăng ký chưa bắt đầu. Mở đăng ký từ: {activeTerm.RegistrationStart:dd/MM/yyyy}"));

            if (now > activeTerm.RegistrationEnd)
                return (null, ServiceResult.Fail($"Đã hết hạn đăng ký. Hạn cuối: {activeTerm.RegistrationEnd:dd/MM/yyyy}"));

            // Kiem tra sinh vien da dang ky cho dot nay chua
            var alreadyRegistered = await _context.Registrations
                .AnyAsync(r => r.StudentId == userId && r.TermId == activeTerm.Id);

            if (alreadyRegistered)
                return (null, ServiceResult.Fail("Bạn đã đăng ký thực tập cho đợt này rồi."));

            return (activeTerm, null);
        }

        public async Task<ServiceResult> RegisterInternal(Guid userId, int companyId, string position)
        {
            // Kiem tra dieu kien dang ky
            var (activeTerm, error) = await ValidateRegistration(userId);
            if (error != null) return error;

            // Kiem tra doanh nghiep ton tai
            var company = await _context.Companies.FindAsync(companyId);
            if (company == null)
                return ServiceResult.Fail("Không tìm thấy doanh nghiệp.");

            var registration = new Registration
            {
                StudentId = userId,
                TermId = activeTerm!.Id,
                CompanyId = companyId,
                Position = position,
                Status = RegistrationStatus.Pending,
                CreatedDate = DateTime.UtcNow
            };

            _context.Registrations.Add(registration);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Sinh vien {UserId} da dang ky thuc tap noi bo tai cong ty {CompanyId}", userId, companyId);
            return ServiceResult.Ok($"Đã đăng ký thực tập tại {company.Name} thành công! Vui lòng chờ duyệt.");
        }

        public async Task<ServiceResult> RegisterExternal(Guid userId,
            string companyName, string address, string? taxCode,
            string contactPerson, string? positionTitle,
            string phone, string email, string position, string? note)
        {
            // Kiem tra dieu kien dang ky
            var (activeTerm, error) = await ValidateRegistration(userId);
            if (error != null) return error;

            // Tim hoac tao doanh nghiep (co chong trung lap)
            var company = await _companyService.FindOrCreateExternal(
                companyName, address, taxCode, contactPerson, email, phone);

            var registration = new Registration
            {
                StudentId = userId,
                TermId = activeTerm!.Id,
                CompanyId = company.Id,
                ExternalCompanyName = companyName,
                ExternalCompanyAddress = address,
                Position = position,
                Status = RegistrationStatus.Pending,
                CreatedDate = DateTime.UtcNow
            };

            _context.Registrations.Add(registration);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Sinh vien {UserId} da dang ky thuc tap ben ngoai tai {CompanyName}", userId, companyName);
            return ServiceResult.Ok($"Đã đăng ký thực tập tại {companyName} thành công! Vui lòng chờ duyệt.");
        }

        public async Task<ServiceResult> CancelRegistration(Guid userId, Guid registrationId)
        {
            var registration = await _context.Registrations
                .FirstOrDefaultAsync(r => r.Id == registrationId && r.StudentId == userId);

            if (registration == null)
                return ServiceResult.Fail("Không tìm thấy đơn đăng ký.");

            if (registration.Status != RegistrationStatus.Pending)
                return ServiceResult.Fail("Chỉ có thể hủy đơn đăng ký đang chờ duyệt.");

            _context.Registrations.Remove(registration);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Sinh vien {UserId} da huy don dang ky {RegId}", userId, registrationId);
            return ServiceResult.Ok("Đã hủy đơn đăng ký. Bạn có thể đăng ký lại.");
        }

        public async Task<Registration?> GetMyRegistration(Guid userId)
        {
            return await _context.Registrations
                .Include(r => r.Company)
                .Include(r => r.Term)
                .Where(r => r.StudentId == userId)
                .OrderByDescending(r => r.CreatedDate)
                .FirstOrDefaultAsync();
        }

        public async Task<ServiceResult> ApproveRegistration(Guid registrationId)
        {
            var registration = await _context.Registrations
                .Include(r => r.Student).ThenInclude(s => s!.Class)
                .FirstOrDefaultAsync(r => r.Id == registrationId);

            if (registration == null)
                return ServiceResult.Fail("Không tìm thấy đơn đăng ký.");

            registration.Status = RegistrationStatus.Approved;
            registration.UpdatedDate = DateTime.UtcNow;

            // Tu dong gan GVHD tu lop neu lop da phan cong
            if (registration.LecturerId == null && registration.Student?.Class?.LecturerId != null)
            {
                registration.LecturerId = registration.Student.Class.LecturerId;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Don dang ky {RegId} da duoc duyet", registrationId);
            return ServiceResult.Ok();
        }

        public async Task<ServiceResult> RejectRegistration(Guid registrationId, string reason)
        {
            var registration = await _context.Registrations.FindAsync(registrationId);
            if (registration == null)
                return ServiceResult.Fail("Không tìm thấy đơn đăng ký.");

            registration.Status = RegistrationStatus.Rejected;
            registration.RejectionReason = reason;
            registration.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Don dang ky {RegId} da bi tu choi", registrationId);
            return ServiceResult.Ok();
        }

        public async Task<ServiceResult> AssignLecturer(Guid registrationId, Guid lecturerId)
        {
            var registration = await _context.Registrations.FindAsync(registrationId);
            if (registration == null)
                return ServiceResult.Fail("Không tìm thấy đơn đăng ký.");

            registration.LecturerId = lecturerId;
            registration.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ServiceResult.Ok();
        }
    }
}
