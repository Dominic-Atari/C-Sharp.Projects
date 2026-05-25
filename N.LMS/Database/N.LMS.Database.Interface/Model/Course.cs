using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace N.LMS.Database.Interface.Model;

[Index(nameof(InstructorId))]
public class Course
{
    [Key] public Guid CourseId { get; set; } = Guid.NewGuid();
    [Required, MaxLength(200)] public string Title { get; set; } = string.Empty;
    [MaxLength(4000)] public string? Description { get; set; }
    [Required] public Guid InstructorId { get; set; }
    public CourseStatus Status { get; set; } = CourseStatus.Draft;
    public DateTime? PublishedUtc { get; set; }
    public bool Deleted { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedUtc { get; set; } = DateTime.UtcNow;

    public User? Instructor { get; set; }
    public ICollection<Module> Modules { get; set; } = new List<Module>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
