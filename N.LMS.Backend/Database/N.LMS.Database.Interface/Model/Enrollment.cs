using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace N.LMS.Database.Interface.Model;

[Index(nameof(UserId), nameof(CourseId), IsUnique = true)]
public class Enrollment
{
    [Key] public Guid EnrollmentId { get; set; } = Guid.NewGuid();
    [Required] public Guid UserId { get; set; }
    [Required] public Guid CourseId { get; set; }
    public EnrollmentRole Role { get; set; } = EnrollmentRole.Student;
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;
    public DateTime EnrolledUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedUtc { get; set; }
    public decimal ProgressPercent { get; set; }
    public bool Deleted { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedUtc { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
    public Course? Course { get; set; }
}
