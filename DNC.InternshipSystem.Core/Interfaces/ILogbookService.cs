using DNC.InternshipSystem.Core.Entities;
using DNC.InternshipSystem.Core.Models;

namespace DNC.InternshipSystem.Core.Interfaces
{
    /// <summary>
    /// Dich vu xu ly nghiep vu nhat ky thuc tap (Logbook)
    /// Bao gom: tao logbook, xem danh sach, binh luan, phan tich AI
    /// </summary>
    public interface ILogbookService
    {
        /// <summary>
        /// Tao nhat ky tuan moi cho sinh vien
        /// Tu dong goi AI phan tich sau khi luu
        /// </summary>
        Task<ServiceResult> CreateLogbook(Guid userId, int weekNumber,
            DateTime startDate, DateTime endDate, string content);

        /// <summary>
        /// Lay danh sach logbook theo Registration
        /// </summary>
        Task<List<Logbook>> GetLogbooks(Guid registrationId);

        /// <summary>
        /// Giang vien them nhan xet vao logbook
        /// Kiem tra quyen: GV phai duoc phan cong huong dan SV nay
        /// </summary>
        Task<ServiceResult> AddComment(Guid logbookId, Guid lecturerId, string comment);

        /// <summary>
        /// Goi AI phan tich noi dung logbook
        /// Kiem tra quyen: GV phai duoc phan cong huong dan SV nay
        /// </summary>
        Task<ServiceResult<string>> AnalyzeWithAI(Guid logbookId, Guid lecturerId);

        /// <summary>
        /// Lay don dang ky duoc duyet moi nhat cua sinh vien
        /// </summary>
        Task<Registration?> GetActiveRegistration(Guid userId);
    }
}
