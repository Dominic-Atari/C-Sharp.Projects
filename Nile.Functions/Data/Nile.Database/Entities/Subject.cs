namespace Nile.Database.Entities;

public enum EducationStage
{
    PreSchool = 1,
    Primary = 2,
    MiddleSchool = 3,
    HighSchool = 4,
    Undergraduate = 5,
    Graduate = 6,
    Professional = 7
}

public class Subject
{
    public Guid SubjectId { get; set; }
    public Guid SchoolId { get; set; }
    public string Name { get; set; } = null!;
    public EducationStage Stage { get; set; }
    public string? Description { get; set; }
    public Guid? SubLevelId { get; set; }
    public SubLevel? SubLevel { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation
    public ICollection<Course> Courses { get; set; } = new List<Course>();
    public ICollection<TeacherSubject> TeacherSubjects { get; set; } = new List<TeacherSubject>();
}
