namespace DNC.InternshipSystem.Core.Entities
{
    public class Major
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int BatchId { get; set; }
        public bool IsActive { get; set; } = true;

        // Quan he navigation
        public virtual StudentBatch? Batch { get; set; }
        public virtual ICollection<Class> Classes { get; set; } = new List<Class>();
    }
}
