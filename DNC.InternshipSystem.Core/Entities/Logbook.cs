using System;

namespace DNC.InternshipSystem.Core.Entities
{
    public class Logbook
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid RegistrationId { get; set; }
        public int WeekNumber { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? EvidenceImageUrl { get; set; }

        // Ket qua phan tich tu dong
        public string? AISummary { get; set; }
        public string? AiSuggestions { get; set; }
        public int? AiScore { get; set; }
        public bool AIWarning { get; set; } = false;
        public string? AIWarningDetails { get; set; }

        // Nhan xet cua giang vien
        public string? LecturerComment { get; set; }
        public DateTime SubmittedDate { get; set; } = DateTime.UtcNow;

        // Yeu cau nop lai tu SV
        public bool ResubmitRequested { get; set; } = false;
        public string? ResubmitReason { get; set; }

        // Quan he navigation
        public Registration? Registration { get; set; }
    }
}
