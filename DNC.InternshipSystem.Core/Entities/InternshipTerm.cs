using System;

namespace DNC.InternshipSystem.Core.Entities
{
    public class InternshipTerm
    {
        public int Id { get; set; }
        public string TermName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime RegistrationDeadline { get; set; }
        public DateTime ReportDeadline { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
