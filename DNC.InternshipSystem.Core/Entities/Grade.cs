using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace DNC.InternshipSystem.Core.Entities
{
    public class Grade
    {
        public Guid RegistrationId { get; set; }
        public double? CompanyScore { get; set; }
        public double? InstructorScore { get; set; }
        
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        // Cot tinh toan: (CompanyScore * 0.4) + (InstructorScore * 0.6)
        public double? FinalScore { get; private set; } // Cot tinh toan
        
        public string? Note { get; set; }
        public DateTime GradedDate { get; set; } = DateTime.Now;

        public Registration? Registration { get; set; }
    }
}
