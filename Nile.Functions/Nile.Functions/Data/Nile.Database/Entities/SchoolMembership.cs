namespace Nile.Database.Entities;

public enum SchoolRole
{
    HeadTeacher = 1,
    Student = 2,
    Teacher = 3
}

public class SchoolMembership
{
    public Guid SchoolMembershipId { get; set; }
    public Guid SchoolId { get; set; }
    public Guid UserId { get; set; }
    public SchoolRole RoleInSchool { get; set; }
    public Guid? StageId { get; set; }
    public Guid? SubLevelId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public School School { get; set; } = null!;
    public User User { get; set; } = null!;
    public SubLevel? SubLevel { get; set; }
}
