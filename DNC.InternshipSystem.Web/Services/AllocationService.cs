using DNC.InternshipSystem.Core.Enums;
using DNC.InternshipSystem.Core.Interfaces;
using DNC.InternshipSystem.Core.Models;
using DNC.InternshipSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DNC.InternshipSystem.Web.Services
{
    /// <summary>
    /// Xu ly nghiep vu phan cong giang vien cho lop
    /// Dam bao dong bo 2 chieu giua Class.LecturerId va Registration.LecturerId
    /// </summary>
    public class AllocationService : IAllocationService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AllocationService> _logger;

        public AllocationService(AppDbContext context, ILogger<AllocationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ServiceResult<string>> AssignLecturer(string classId, Guid? lecturerId)
        {
            var cls = await _context.Classes.FindAsync(classId);
            if (cls == null)
                return ServiceResult<string>.Fail("Không tìm thấy lớp.");

            cls.LecturerId = lecturerId;

            // Dong bo Registration.LecturerId cho tat ca SV da duyet trong lop nay
            if (lecturerId.HasValue)
            {
                await SyncRegistrationsForClass(classId, lecturerId);
            }

            await _context.SaveChangesAsync();

            // Lay ten GV de tra ve hien thi
            string lecturerName = "Chưa phân công";
            if (lecturerId.HasValue)
            {
                var lec = await _context.Lecturers
                    .Include(l => l.User)
                    .FirstOrDefaultAsync(l => l.UserId == lecturerId.Value);
                if (lec != null)
                {
                    string rank = !string.IsNullOrEmpty(lec.AcademicRank)
                        ? lec.AcademicRank + ". " : "";
                    lecturerName = rank + (lec.User?.FullName ?? lec.LecturerCode);
                }
            }

            _logger.LogInformation("Phan cong GV {LecId} cho lop {ClassId}", lecturerId, classId);
            return ServiceResult<string>.Ok(lecturerName);
        }

        public async Task<ServiceResult<int>> BulkAssign(string[] classIds, Guid lecturerId)
        {
            if (classIds == null || classIds.Length == 0)
                return ServiceResult<int>.Fail("Chưa chọn lớp nào.");

            var lecturer = await _context.Lecturers
                .Include(l => l.User)
                .FirstOrDefaultAsync(l => l.UserId == lecturerId);
            if (lecturer == null)
                return ServiceResult<int>.Fail("Giảng viên không tồn tại.");

            int count = 0;
            foreach (var cid in classIds)
            {
                var cls = await _context.Classes.FindAsync(cid);
                if (cls != null)
                {
                    cls.LecturerId = lecturerId;
                    count++;

                    // Dong bo Registration cho tung lop
                    await SyncRegistrationsForClass(cid, lecturerId);
                }
            }

            await _context.SaveChangesAsync();

            string rank = !string.IsNullOrEmpty(lecturer.AcademicRank)
                ? lecturer.AcademicRank + ". " : "";
            string name = rank + (lecturer.User?.FullName ?? lecturer.LecturerCode);

            _logger.LogInformation("Phan cong GV {LecId} cho {Count} lop", lecturerId, count);
            return ServiceResult<int>.Ok(count, $"Đã phân công {count} lớp cho {name}");
        }

        public async Task<ServiceResult> RemoveAssignment(string classId)
        {
            var cls = await _context.Classes.FindAsync(classId);
            if (cls == null)
                return ServiceResult.Fail("Không tìm thấy lớp.");

            var oldLecturerId = cls.LecturerId;
            cls.LecturerId = null;

            // Dong bo: xoa LecturerId trong Registration cua SV thuoc lop nay
            // Chi xoa neu LecturerId trung voi GV cu cua lop
            if (oldLecturerId.HasValue)
            {
                var regsInClass = await _context.Registrations
                    .Include(r => r.Student)
                    .Where(r => r.Student!.ClassId == classId
                        && r.Status == RegistrationStatus.Approved
                        && r.LecturerId == oldLecturerId)
                    .ToListAsync();

                foreach (var reg in regsInClass)
                {
                    reg.LecturerId = null;
                    reg.UpdatedDate = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Da go phan cong GV khoi lop {ClassId}", classId);
            return ServiceResult.Ok();
        }

        /// <summary>
        /// Dong bo Registration.LecturerId cho tat ca SV da duyet trong lop
        /// </summary>
        private async Task SyncRegistrationsForClass(string classId, Guid? lecturerId)
        {
            var regsInClass = await _context.Registrations
                .Include(r => r.Student)
                .Where(r => r.Student!.ClassId == classId
                    && r.Status == RegistrationStatus.Approved)
                .ToListAsync();

            foreach (var reg in regsInClass)
            {
                reg.LecturerId = lecturerId;
                reg.UpdatedDate = DateTime.UtcNow;
            }
        }
    }
}
