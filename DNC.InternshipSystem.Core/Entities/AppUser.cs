using Microsoft.AspNetCore.Identity;
using System;

namespace DNC.InternshipSystem.Core.Entities
{
    public class AppUser : IdentityUser<Guid>
    {
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation property
        public Student? Student { get; set; }
        public Lecturer? Lecturer { get; set; }
    }
}
