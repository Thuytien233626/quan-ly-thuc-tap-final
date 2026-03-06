using System;
using DNC.InternshipSystem.Core.Enums;

namespace DNC.InternshipSystem.Core.Entities
{
    public class Student
    {
        public Guid UserId { get; set; } // PK, FK den AppUser
        public string StudentCode { get; set; } = string.Empty;
        public string? ClassId { get; set; }
         public int OrderNumber { get; set; } = 0; // So thu tu trong lop (tu file Excel)
        public Gender Gender { get; set; } = Gender.Male;
        public DateTime? DateOfBirth { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? VerifiedToken { get; set; }
        public string? ProfileImage { get; set; } // Duong dan den anh dai dien cua sinh vien

        // Quan he navigation
        public AppUser? User { get; set; }
        public Class? Class { get; set; }
    }
}
