namespace DNC.InternshipSystem.Core.Entities
{
    public class Class
    {
        public string Id { get; set; } = string.Empty; // Mã lớp, VD: DH23TIN03
        public string Name { get; set; } = string.Empty;
        public int DepartmentId { get; set; }
        
        // Navigation property
        public Department? Department { get; set; }
    }
}
