using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace N.LMS.Database.Interface.Model;

[Index(nameof(Email), IsUnique = true)]
public class User
{
    [Key] public Guid UserId { get; set; } = Guid.NewGuid();
    [Required, MaxLength(100)] public string FirstName { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string LastName { get; set; } = string.Empty;
    [Required, MaxLength(320)] public string Email { get; set; } = string.Empty;
    [Required, MaxLength(512)] public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Student;
    public Guid? CohortId { get; set; }
    public bool Deleted { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedUtc { get; set; } = DateTime.UtcNow;

    public Cohort? Cohort { get; set; }
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
