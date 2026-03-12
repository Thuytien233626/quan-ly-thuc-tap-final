using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace DNC.InternshipSystem.Core.Entities
{
    public class Grade
    {
        public Guid RegistrationId { get; set; }
        public double? CompanyScore { get; set; }
        public double? InstructorScore { get; set; }

        // Cot tinh toan: (CompanyScore * 0.4) + (InstructorScore * 0.6)
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public double? FinalScore { get; private set; }

        public string? Note { get; set; }
        public DateTime GradedDate { get; set; } = DateTime.UtcNow;

        // Quan he navigation
        public Registration? Registration { get; set; }
    }
}
