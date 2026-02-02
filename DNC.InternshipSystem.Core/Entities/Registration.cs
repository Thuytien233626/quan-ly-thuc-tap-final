using System;

namespace DNC.InternshipSystem.Core.Entities
{
    public class Registration
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid StudentId { get; set; }
        public int TermId { get; set; }
        public int? CompanyId { get; set; }
        public string? ExternalCompanyName { get; set; }
        public string? ExternalCompanyAddress { get; set; }
        public Guid? LecturerId { get; set; }
        public string Position { get; set; } = string.Empty;
        public int Status { get; set; } = 0; // 0: Pending, 1: Approved, 2: Rejected, 3: Completed
        public string? RejectionReason { get; set; }
        public string? EvidenceUrl { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }

        public Student? Student { get; set; }
        public InternshipTerm? Term { get; set; }
        public Company? Company { get; set; }
        public Lecturer? Lecturer { get; set; }
    }
}
