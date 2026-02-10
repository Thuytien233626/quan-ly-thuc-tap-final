using System;
using DNC.InternshipSystem.Core.Enums;

namespace DNC.InternshipSystem.Core.Entities
{
    /// <summary>
    /// Dot thuc tap
    /// </summary>
    public class InternshipTerm
    {
        public int Id { get; set; }
        
        /// <summary>
        /// Ten dot: "TTTN K11", "TTTN K10 Bo sung"
        /// </summary>
        public string TermName { get; set; } = string.Empty;
        
        /// <summary>
        /// Nam hoc: "2025-2026"
        /// </summary>
        public string AcademicYear { get; set; } = string.Empty;
        
        /// <summary>
        /// Hoc ky (mac dinh HK3)
        /// </summary>
        public int Semester { get; set; } = 3;
        
        /// <summary>
        /// Thoi luong thuc tap (tuan): 4, 6, 8, 12
        /// </summary>
        public int DurationInWeeks { get; set; } = 8;
        
        // Khoa ngoai - Khoa hoc
        public int BatchId { get; set; }
        public virtual StudentBatch? Batch { get; set; }
        
        // Moc thoi gian
        public DateTime RegistrationStart { get; set; }
        public DateTime RegistrationEnd { get; set; }
        public DateTime InternshipStart { get; set; }
        public DateTime ReportDeadline { get; set; }
        public DateTime EndDate { get; set; }
        
        /// <summary>
        /// Trang thai dot
        /// </summary>
        public TermStatus Status { get; set; } = TermStatus.Draft;
        
        /// <summary>
        /// Dang hoat dong
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}
