namespace Nile.Database.Entities;

public class TeacherSubject
{
    public Guid TeacherSubjectId { get; set; }
    public Guid UserId { get; set; }
    public Guid SchoolId { get; set; }
    public Guid SubjectId { get; set; }
    public DateTime AssignedAt { get; set; }

    // Navigation
    public User Teacher { get; set; } = null!;
    public School School { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
}
