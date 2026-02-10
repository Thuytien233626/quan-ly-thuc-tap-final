namespace DNC.InternshipSystem.Web.Areas.Admin.Models
{
    public class ImportPreviewViewModel
    {
        // Detected columns info
        public bool HasMSSV { get; set; }
        public bool HasHoDem { get; set; }
        public bool HasTen { get; set; }
        public bool HasGioiTinh { get; set; }
        public bool HasNgaySinh { get; set; }
        public bool HasLopHoc { get; set; }
        
        public int HeaderRow { get; set; }
        public int TotalRows { get; set; }
        public int ValidRows { get; set; }
        public int SkippedRows { get; set; }
        public int ExistingStudents { get; set; }
        
        public string FileName { get; set; } = "";
        public string TempFilePath { get; set; } = "";
        
        public List<StudentPreviewItem> Students { get; set; } = new();
        
        public bool AllRequiredColumnsFound => HasMSSV && HasHoDem && HasTen;
    }
    
    public class StudentPreviewItem
    {
        public int RowNumber { get; set; }
        public string StudentCode { get; set; } = "";
        public string LastName { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string FullName => $"{LastName} {FirstName}".Trim();
        public string Gender { get; set; } = "";
        public string DateOfBirth { get; set; } = "";
        public string ClassCode { get; set; } = "";
        public bool ClassExists { get; set; }
        public bool AlreadyExists { get; set; }
        public string Status { get; set; } = "Sẵn sàng";
    }
}
