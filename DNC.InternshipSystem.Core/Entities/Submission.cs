using System;
using System.ComponentModel.DataAnnotations;

namespace DNC.InternshipSystem.Core.Entities
{
    public class Submission
    {
        public int Id { get; set; }
        public Guid RegistrationId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string? Note { get; set; }
        public DateTime SubmittedDate { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Pending";

        // GV duyet bao cao
        public string? LecturerComment { get; set; }
        public DateTime? ReviewedDate { get; set; }

        // Quan he navigation
        public Registration? Registration { get; set; }
    }
}