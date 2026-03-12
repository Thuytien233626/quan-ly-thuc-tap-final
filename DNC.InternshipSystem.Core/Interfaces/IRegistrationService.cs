using DNC.InternshipSystem.Core.Entities;
using DNC.InternshipSystem.Core.Models;

namespace DNC.InternshipSystem.Core.Interfaces
{
    /// <summary>
    /// Dich vu xu ly nghiep vu dang ky thuc tap
    /// Bao gom: dang ky noi bo, dang ky ben ngoai, huy don, duyet/tu choi
    /// </summary>
    public interface IRegistrationService
    {
        /// <summary>
        /// Dang ky thuc tap tai doanh nghiep doi tac (noi bo)
        /// </summary>
        Task<ServiceResult> RegisterInternal(Guid userId, int companyId, string position);

        /// <summary>
        /// Dang ky thuc tap tai doanh nghiep ben ngoai (sinh vien tu tim)
        /// </summary>
        Task<ServiceResult> RegisterExternal(Guid userId, string companyName, string address,
            string? taxCode, string contactPerson, string? positionTitle,
            string phone, string email, string position, string? note);

        /// <summary>
        /// Huy don dang ky dang cho duyet
        /// </summary>
        Task<ServiceResult> CancelRegistration(Guid userId, Guid registrationId);

        /// <summary>
        /// Lay don dang ky moi nhat cua sinh vien
        /// </summary>
        Task<Registration?> GetMyRegistration(Guid userId);

        /// <summary>
        /// Duyet don dang ky (Admin)
        /// Tu dong gan GVHD tu lop neu lop da phan cong
        /// </summary>
        Task<ServiceResult> ApproveRegistration(Guid registrationId);

        /// <summary>
        /// Tu choi don dang ky (Admin)
        /// </summary>
        Task<ServiceResult> RejectRegistration(Guid registrationId, string reason);

        /// <summary>
        /// Gan giang vien huong dan cho don dang ky cu the
        /// </summary>
        Task<ServiceResult> AssignLecturer(Guid registrationId, Guid lecturerId);
    }
}
