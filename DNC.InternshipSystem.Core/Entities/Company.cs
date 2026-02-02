namespace DNC.InternshipSystem.Core.Entities
{
    public class Company
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? TaxCode { get; set; }
        public string? ContactPerson { get; set; }
        public string? ContactEmail { get; set; }
        public int Status { get; set; } = 1; // 1: Verified, 0: Unverified
    }
}
