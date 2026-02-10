namespace DNC.InternshipSystem.Core.Entities
{
    public class Class
    {
        public string Id { get; set; } = string.Empty; // Ma lop, VD: DH23TIN03
        public string Name { get; set; } = string.Empty;
        public int MajorId { get; set; }
        public Guid? LecturerId { get; set; } // GVHD - nullable vi co the chua phan cong
        public bool IsActive { get; set; } = true;
        
        // Quan he navigation
        public virtual Major? Major { get; set; }
        public virtual Lecturer? Lecturer { get; set; }
        public virtual ICollection<DNC.InternshipSystem.Core.Entities.Student> Students { get; set; } = new List<DNC.InternshipSystem.Core.Entities.Student>();
    }
}

