using DNC.InternshipSystem.Core.Models;

namespace DNC.InternshipSystem.Core.Interfaces
{
    /// <summary>
    /// Dich vu xu ly nghiep vu phan cong giang vien huong dan cho lop
    /// Khi gan GV cho lop -> tu dong dong bo vao Registration cua SV trong lop do
    /// </summary>
    public interface IAllocationService
    {
        /// <summary>
        /// Phan cong 1 GV cho 1 lop cụ thể
        /// Dong bo Registration.LecturerId cho tat ca SV da duyet trong lop
        /// </summary>
        Task<ServiceResult<string>> AssignLecturer(string classId, Guid? lecturerId);

        /// <summary>
        /// Phan cong GV cho nhieu lop cung luc
        /// </summary>
        Task<ServiceResult<int>> BulkAssign(string[] classIds, Guid lecturerId);

        /// <summary>
        /// Go GV khoi lop (xoa phan cong)
        /// Dong bo xoa LecturerId trong Registration cua SV da duyet
        /// </summary>
        Task<ServiceResult> RemoveAssignment(string classId);
    }
}
