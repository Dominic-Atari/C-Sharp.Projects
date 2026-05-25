using System.ComponentModel.DataAnnotations;

namespace N.LMS.Database.Interface.Model;

public class Cohort
{
    [Key] public Guid CohortId { get; set; } = Guid.NewGuid();
    [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
    [MaxLength(2000)] public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool Deleted { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedUtc { get; set; } = DateTime.UtcNow;

    public ICollection<User> Members { get; set; } = new List<User>();
}
