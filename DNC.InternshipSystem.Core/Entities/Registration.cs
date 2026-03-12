using System;
using DNC.InternshipSystem.Core.Enums;

namespace DNC.InternshipSystem.Core.Entities
{
    public class Registration
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid StudentId { get; set; }
        public int TermId { get; set; }
        public int? CompanyId { get; set; }

        // Thong tin doanh nghiep ben ngoai (sinh vien tu tim)
        public string? ExternalCompanyName { get; set; }
        public string? ExternalCompanyAddress { get; set; }

        public Guid? LecturerId { get; set; }
        public string Position { get; set; } = string.Empty;

        // Trang thai don dang ky
        public RegistrationStatus Status { get; set; } = RegistrationStatus.Pending;

        public string? RejectionReason { get; set; }
        public string? EvidenceUrl { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }

        // Diem thuc tap
        public double? CompanyScore { get; set; }
        public double? InstructorScore { get; set; }
        public double? FinalScore { get; set; }

        // Quan he navigation
        public Student? Student { get; set; }
        public InternshipTerm? Term { get; set; }
        public Company? Company { get; set; }
        public Lecturer? Lecturer { get; set; }
        public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
    }
}
