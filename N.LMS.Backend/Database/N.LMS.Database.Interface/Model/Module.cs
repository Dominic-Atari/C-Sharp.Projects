using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace N.LMS.Database.Interface.Model;

[Index(nameof(CourseId), nameof(SortOrder))]
public class Module
{
    [Key] public Guid ModuleId { get; set; } = Guid.NewGuid();
    [Required] public Guid CourseId { get; set; }
    [Required, MaxLength(200)] public string Title { get; set; } = string.Empty;
    [MaxLength(4000)] public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool Deleted { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedUtc { get; set; } = DateTime.UtcNow;

    public Course? Course { get; set; }
    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
}
