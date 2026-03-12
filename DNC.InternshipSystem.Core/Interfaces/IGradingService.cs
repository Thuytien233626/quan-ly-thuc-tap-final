using DNC.InternshipSystem.Core.Entities;
using DNC.InternshipSystem.Core.Models;

namespace DNC.InternshipSystem.Core.Interfaces
{
    /// <summary>
    /// Dich vu xu ly nghiep vu cham diem thuc tap
    /// Cong thuc: Diem tong = (Diem DN * 0.4) + (Diem GVHD * 0.6)
    /// </summary>
    public interface IGradingService
    {
        /// <summary>
        /// Tinh diem tong ket tu 2 dau diem
        /// </summary>
        double? CalculateFinalScore(double? companyScore, double? instructorScore);

        /// <summary>
        /// Cap nhat diem tu Admin (ca 2 dau diem)
        /// </summary>
        Task<ServiceResult<double?>> UpdateScoresFromAdmin(
            Guid registrationId, double? companyScore, double? instructorScore);

        /// <summary>
        /// Cap nhat diem tu Giang vien (chi diem GVHD)
        /// Kiem tra quyen: GV phai duoc phan cong huong dan SV nay
        /// </summary>
        Task<ServiceResult> UpdateScoresFromLecturer(
            Guid registrationId, Guid lecturerId,
            double? instructorScore, string? note);

        /// <summary>
        /// Xuat bang diem ra file Excel
        /// </summary>
        Task<byte[]> ExportToExcel(Guid? lecturerId, int? majorId, string? search);
    }
}
