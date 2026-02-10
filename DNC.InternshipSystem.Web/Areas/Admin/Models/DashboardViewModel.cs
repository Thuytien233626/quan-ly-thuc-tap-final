using System.Collections.Generic;

namespace DNC.InternshipSystem.Web.Areas.Admin.Models
{
    public class DashboardViewModel
    {
        // Stat Cards
        public int ActiveTermCount { get; set; }
        public int TotalStudents { get; set; }
        public int ActiveStudents { get; set; } // SV đang thực tập (có Registration approved)
        public int TotalLecturers { get; set; }
        public int ActiveLecturers { get; set; } // GV đang hướng dẫn
        public int TotalCompanies { get; set; }
        public int PendingRegistrations { get; set; }

        // Grading Charts
        public int PassedCount { get; set; }       // FinalScore >= 5.0
        public int FailedCount { get; set; }       // FinalScore < 5.0
        public int NotGradedCount { get; set; }    // FinalScore == null (but assigned)
        
        // Score Distribution (5 ranges: 0-2, 2-4, 4-6, 6-8, 8-10)
        public int[] ScoreDistribution { get; set; } = new int[5];

        // Recent Registrations
        public List<RecentRegistrationItem> RecentRegistrations { get; set; } = new();
    }

    public class RecentRegistrationItem
    {
        public string StudentName { get; set; } = "";
        public string StudentCode { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public string MajorName { get; set; } = "";
        public DateTime CreatedDate { get; set; }
        public int Status { get; set; } // 0=Pending, 1=Approved, -1=Rejected
    }
}
