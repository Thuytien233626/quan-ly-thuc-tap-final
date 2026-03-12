using DNC.InternshipSystem.Core.Entities;
using DNC.InternshipSystem.Core.Models;

namespace DNC.InternshipSystem.Core.Interfaces
{
    /// <summary>
    /// Dich vu xu ly nghiep vu doanh nghiep
    /// Bao gom: tim kiem, phan trang, tao doanh nghiep ben ngoai (co chong trung lap)
    /// </summary>
    public interface ICompanyService
    {
        /// <summary>
        /// Lay danh sach doanh nghiep doi tac (noi bo) co phan trang va tim kiem
        /// </summary>
        Task<PagedResult<Company>> GetApprovedCompanies(string? search, string? province, int page, int pageSize);

        /// <summary>
        /// Lay danh sach tinh/thanh co doanh nghiep doi tac
        /// </summary>
        Task<List<string>> GetProvinces();

        /// <summary>
        /// Tim hoac tao doanh nghiep ben ngoai
        /// Kiem tra trung lap theo Ten + Ma so thue truoc khi tao moi
        /// </summary>
        Task<Company> FindOrCreateExternal(string name, string address,
            string? taxCode, string? contactPerson, string? contactEmail, string? phone);
    }

    /// <summary>
    /// Ket qua phan trang chung cho danh sach
    /// </summary>
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
    }
}
