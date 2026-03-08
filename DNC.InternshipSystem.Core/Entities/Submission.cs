using System;
using System.ComponentModel.DataAnnotations;

namespace DNC.InternshipSystem.Core.Entities
{
    public class Submission
{
    public int Id { get; set; }

    public Guid RegistrationId { get; set; }

    public string Title { get; set; }

    public string Type { get; set; }

    public string FilePath { get; set; }

    public string Note { get; set; }

    public DateTime SubmittedDate { get; set; }

    public string Status { get; set; }

    public Registration Registration { get; set; }
}
}