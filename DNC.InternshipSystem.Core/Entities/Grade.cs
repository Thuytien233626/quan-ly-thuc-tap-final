using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace DNC.InternshipSystem.Core.Entities
{
    public class Grade
    {
        public Guid RegistrationId { get; set; }
        public double? CompanyScore { get; set; }
        public double? InstructorScore { get; set; }
        public double? ReportScore { get; set; }
        
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public double? FinalScore { get; private set; } // Computed column
        
        public string? Note { get; set; }
        public DateTime GradedDate { get; set; } = DateTime.Now;

        public Registration? Registration { get; set; }
    }
}
