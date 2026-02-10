namespace DNC.InternshipSystem.Core.Entities
{
    /// <summary>
    /// Khoa hoc sinh vien (K9, K10, K11...)
    /// </summary>
    public class StudentBatch
    {
        public int Id { get; set; }
        
        /// <summary>
        /// Ma khoa: K9, K10, K11
        /// </summary>
        public string BatchCode { get; set; } = string.Empty;
        
        /// <summary>
        /// Ma lop: DH21, DH22, DH23
        /// </summary>
        public string ClassCode { get; set; } = string.Empty;
        
        /// <summary>
        /// Nam nhap hoc: 2021, 2022, 2023
        /// </summary>
        public int EnrollmentYear { get; set; }

        /// <summary>
        /// Nam tot nghiep du kien: 2025, 2026, 2027
        /// </summary>
        public int GraduationYear { get; set; }
        
        /// <summary>
        /// Trang thai hoat dong
        /// </summary>
        public bool IsActive { get; set; } = true;

        // Navigation
        public virtual ICollection<InternshipTerm> InternshipTerms { get; set; } = new List<InternshipTerm>();
        public virtual ICollection<Major> Majors { get; set; } = new List<Major>();
    }
}
