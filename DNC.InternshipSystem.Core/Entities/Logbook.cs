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
        
        // Truong du lieu tu dong phan tich
        public string? AISummary { get; set; }
        public bool AIWarning { get; set; } = false;
        public string? AIWarningDetails { get; set; }
        
        public string? LecturerComment { get; set; }
        public DateTime SubmittedDate { get; set; } = DateTime.Now;

        public Registration? Registration { get; set; }
    }
}
