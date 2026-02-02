namespace DNC.InternshipSystem.Core.Entities
{
    public class Department
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public int? EstablishedYear { get; set; }
    }
}
