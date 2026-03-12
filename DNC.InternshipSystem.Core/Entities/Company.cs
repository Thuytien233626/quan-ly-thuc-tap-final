using System.Collections.Generic;
using DNC.InternshipSystem.Core.Enums;

namespace DNC.InternshipSystem.Core.Entities
{
    public class Company
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string? TaxCode { get; set; }
        public string? ContactPerson { get; set; }
        public string? ContactEmail { get; set; }
        public string? PhoneNumber { get; set; }

        // Trang thai xac nhan doanh nghiep
        public CompanyStatus Status { get; set; } = CompanyStatus.Approved;

        // false: Doi tac (nha truong lien ket), true: Ben ngoai (sinh vien tu tim)
        public bool IsExternal { get; set; } = false;

        // Quan he navigation
        public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
    }
}
