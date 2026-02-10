using System;

namespace DNC.InternshipSystem.Core.Entities
{
    public class Lecturer
    {
        public Guid UserId { get; set; } // PK, FK den AppUser
        public string LecturerCode { get; set; } = string.Empty; // Mã GV: GV001, GV002...
        public int DepartmentId { get; set; }
        public string? AcademicRank { get; set; } // ThS, TS, PGS, GS
        public string? Specialization { get; set; } // Chuyên môn: CNPM, Mạng MT...

        // Quan he navigation
        public AppUser? User { get; set; }
        public Department? Department { get; set; }
        public virtual ICollection<Class> Classes { get; set; } = new List<Class>();
    }
}
