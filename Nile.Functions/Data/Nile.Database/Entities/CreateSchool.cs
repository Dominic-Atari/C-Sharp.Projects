namespace Nile.Database.Entities;

public class School
{
    public Guid SchoolId { get; set; }
    public string SchoolName { get; set; } = null!;
    public string SchoolAddress { get; set; } = null!;
    public string City { get; set; } = null!;
    public string State { get; set; } = null!;
    public string Country { get; set; } = null!;
    public string County { get; set; } = null!;
    public string ZipCode { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string ImageUrl { get; set; } = null!;
    public Guid HeadTeacherUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation
    public User HeadTeacher { get; set; } = null!;
    public ICollection<SchoolMembership> Memberships { get; set; } = new List<SchoolMembership>();
    public ICollection<Course> Courses { get; set; } = new List<Course>();
    
}