using System;

namespace DNC.InternshipSystem.Core.Entities
{
    public class Lecturer
    {
        public Guid UserId { get; set; } // PK, FK to AppUser
        public int DepartmentId { get; set; }
        public int MaxStudents { get; set; } = 20;
        public string? AcademicRank { get; set; }

        // Navigation properties
        public AppUser? User { get; set; }
        public Department? Department { get; set; }
    }
}
