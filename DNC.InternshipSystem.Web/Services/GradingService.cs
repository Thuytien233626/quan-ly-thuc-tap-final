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
    /// Xu ly toan bo nghiep vu cham diem thuc tap
    /// Cong thuc: Diem tong = (Diem DN * 0.4) + (Diem GVHD * 0.6)
    /// Diem duoc luu duy nhat trong Registration (single source of truth)
    /// </summary>
    public class GradingService : IGradingService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<GradingService> _logger;

        // Trong so cac dau diem (co the cau hinh tu appsettings)
        private const double COMPANY_WEIGHT = 0.4;
        private const double INSTRUCTOR_WEIGHT = 0.6;

        public GradingService(AppDbContext context, ILogger<GradingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public double? CalculateFinalScore(double? companyScore, double? instructorScore)
        {
            if (companyScore.HasValue && instructorScore.HasValue)
            {
                return Math.Round(
                    (companyScore.Value * COMPANY_WEIGHT) +
                    (instructorScore.Value * INSTRUCTOR_WEIGHT), 2);
            }
            return null;
        }

        public async Task<ServiceResult<double?>> UpdateScoresFromAdmin(
            Guid registrationId, double? companyScore, double? instructorScore)
        {
            var registration = await _context.Registrations.FindAsync(registrationId);
            if (registration == null)
                return ServiceResult<double?>.Fail("Không tìm thấy sinh viên.");

            // Kiem tra diem hop le (0-10)
            if (companyScore.HasValue && (companyScore < 0 || companyScore > 10))
                return ServiceResult<double?>.Fail("Điểm doanh nghiệp phải từ 0 đến 10.");

            if (instructorScore.HasValue && (instructorScore < 0 || instructorScore > 10))
                return ServiceResult<double?>.Fail("Điểm giảng viên phải từ 0 đến 10.");

            registration.CompanyScore = companyScore;
            registration.InstructorScore = instructorScore;
            registration.FinalScore = CalculateFinalScore(companyScore, instructorScore);
            registration.UpdatedDate = DateTime.UtcNow;

            // Dong bo sang bang Grade (neu ton tai)
            var grade = await _context.Grades
                .FirstOrDefaultAsync(g => g.RegistrationId == registrationId);
            if (grade != null)
            {
                grade.CompanyScore = companyScore;
                grade.InstructorScore = instructorScore;
                grade.GradedDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Cap nhat diem cho Registration {RegId}: DN={CS}, GV={IS}, Tong={FS}",
                registrationId, companyScore, instructorScore, registration.FinalScore);

            return ServiceResult<double?>.Ok(registration.FinalScore);
        }

        public async Task<ServiceResult> UpdateScoresFromLecturer(
            Guid registrationId, Guid lecturerId,
            double? instructorScore, string? note)
        {
            var registration = await _context.Registrations
                .Include(r => r.Student)
                .FirstOrDefaultAsync(r => r.Id == registrationId);

            if (registration == null)
                return ServiceResult.Fail("Không tìm thấy sinh viên.");

            // Kiem tra quyen: GV phai la nguoi phu trach (phan cong truc tiep hoac qua lop)
            var lecturerClassIds = await _context.Classes
                .Where(c => c.LecturerId == lecturerId)
                .Select(c => c.Id)
                .ToListAsync();

            bool isDirectAssigned = registration.LecturerId == lecturerId;
            bool isClassAssigned = registration.Student?.ClassId != null
                && lecturerClassIds.Contains(registration.Student.ClassId);

            if (!isDirectAssigned && !isClassAssigned)
                return ServiceResult.Fail("Bạn không được phân công hướng dẫn sinh viên này.");

            // Kiem tra diem hop le (0-10)
            if (instructorScore.HasValue && (instructorScore < 0 || instructorScore > 10))
                return ServiceResult.Fail("Điểm phải từ 0 đến 10.");

            // Cap nhat diem tren Registration (nguon chinh)
            registration.InstructorScore = instructorScore;
            registration.FinalScore = CalculateFinalScore(registration.CompanyScore, instructorScore);
            registration.UpdatedDate = DateTime.UtcNow;

            // Dong bo sang bang Grade
            var grade = await _context.Grades
                .FirstOrDefaultAsync(g => g.RegistrationId == registrationId);

            if (grade == null)
            {
                grade = new Grade
                {
                    RegistrationId = registrationId,
                    InstructorScore = instructorScore,
                    Note = note,
                    GradedDate = DateTime.UtcNow
                };
                _context.Grades.Add(grade);
            }
            else
            {
                grade.InstructorScore = instructorScore;
                grade.Note = note;
                grade.GradedDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("GV {LecId} da cham diem cho Registration {RegId}", lecturerId, registrationId);
            return ServiceResult.Ok("Đã lưu điểm thành công.");
        }

        public async Task<byte[]> ExportToExcel(Guid? lecturerId, int? majorId, string? search)
        {
            var query = _context.Registrations
                .Include(r => r.Student).ThenInclude(s => s!.User)
                .Include(r => r.Student).ThenInclude(s => s!.Class).ThenInclude(c => c!.Major).ThenInclude(m => m!.Batch)
                .Include(r => r.Company)
                .Include(r => r.Lecturer).ThenInclude(l => l!.User)
                .Where(r => r.Status == RegistrationStatus.Approved && r.LecturerId != null);

            if (lecturerId.HasValue)
                query = query.Where(r => r.LecturerId == lecturerId.Value);

            if (majorId.HasValue)
            {
                var selectedMajor = await _context.Majors.FindAsync(majorId);
                if (selectedMajor != null)
                    query = query.Where(r => r.Student!.Class!.Major!.Name == selectedMajor.Name);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(r =>
                    r.Student!.User!.FullName.Contains(search) ||
                    r.Student.StudentCode.Contains(search));
            }

            var students = await query.ToListAsync();

            using var package = new OfficeOpenXml.ExcelPackage();
            var workSheet = package.Workbook.Worksheets.Add("KetQuaThucTap");

            // Tieu de cot
            workSheet.Cells[1, 1].Value = "STT";
            workSheet.Cells[1, 2].Value = "MSSV";
            workSheet.Cells[1, 3].Value = "Họ tên";
            workSheet.Cells[1, 4].Value = "Lớp";
            workSheet.Cells[1, 5].Value = "Ngành";
            workSheet.Cells[1, 6].Value = "Công ty thực tập";
            workSheet.Cells[1, 7].Value = "GVHD";
            workSheet.Cells[1, 8].Value = "Điểm DN (40%)";
            workSheet.Cells[1, 9].Value = "Điểm GVHD (60%)";
            workSheet.Cells[1, 10].Value = "Tổng kết";
            workSheet.Cells[1, 11].Value = "Xếp loại";

            int recordIndex = 2;
            int stt = 1;

            foreach (var s in students)
            {
                var finalScore = CalculateFinalScore(s.CompanyScore, s.InstructorScore);

                workSheet.Cells[recordIndex, 1].Value = stt++;
                workSheet.Cells[recordIndex, 2].Value = s.Student?.StudentCode;
                workSheet.Cells[recordIndex, 3].Value = s.Student?.User?.FullName;
                workSheet.Cells[recordIndex, 4].Value = s.Student?.Class?.Name;
                workSheet.Cells[recordIndex, 5].Value = s.Student?.Class?.Major?.Name;
                workSheet.Cells[recordIndex, 6].Value = s.Company?.Name;
                workSheet.Cells[recordIndex, 7].Value = s.Lecturer?.User?.FullName;
                workSheet.Cells[recordIndex, 8].Value = s.CompanyScore;
                workSheet.Cells[recordIndex, 9].Value = s.InstructorScore;
                workSheet.Cells[recordIndex, 10].Value = finalScore;

                string status = "";
                if (finalScore.HasValue)
                    status = finalScore >= 5.0 ? "Đạt" : "Không đạt";

                workSheet.Cells[recordIndex, 11].Value = status;
                recordIndex++;
            }

            workSheet.Cells.AutoFitColumns();

            var stream = new MemoryStream();
            package.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
