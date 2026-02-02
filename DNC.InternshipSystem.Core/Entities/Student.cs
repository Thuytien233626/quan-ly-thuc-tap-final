using System;

namespace DNC.InternshipSystem.Core.Entities
{
    public class Student
    {
        public Guid UserId { get; set; } // PK, FK to AppUser
        public string StudentCode { get; set; } = string.Empty;
        public string ClassId { get; set; } = string.Empty;
        public string? Major { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? VerifiedToken { get; set; }

        // Navigation properties
        public AppUser? User { get; set; }
        public Class? Class { get; set; }
    }
}
