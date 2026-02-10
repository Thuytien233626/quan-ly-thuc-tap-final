using System.Collections.Generic;

namespace DNC.InternshipSystem.Core.Entities
{
    public class Company
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty; // Tinh/thanh pho
        public string? TaxCode { get; set; }
        public string? ContactPerson { get; set; }
        public string? ContactEmail { get; set; }
        public string? PhoneNumber { get; set; }
        public int Status { get; set; } = 1; // 1: Da xac nhan (Approved), 0: Dang cho (Waiting for approval)
        public bool IsExternal { get; set; } = false; // false: Doi tac (Nha truong lien ket), true: Ben ngoai (SV tu tim)
        
        // Navigation
        public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
    }
}

