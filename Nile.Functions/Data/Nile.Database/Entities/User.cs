using Nile.Common;

namespace Nile.Database.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    // Navigation properties
    public UserProfile? Profile { get; set; }
    public Passwords? Password { get; set; }
    public ICollection<Post> Posts { get; set; } = new List<Post>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Like> Likes { get; set; } = new List<Like>();
    public ICollection<UserRelationship> Following { get; set; } = new List<UserRelationship>();
    public ICollection<UserRelationship> Followers { get; set; } = new List<UserRelationship>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<Notification> TriggeredNotifications { get; set; } = new List<Notification>();
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    // LMS: optional one-to-one head-teacher ownership and memberships
    public School? School { get; set; }
    public ICollection<SchoolMembership> SchoolMemberships { get; set; } = new List<SchoolMembership>();
    public ICollection<TeacherSubject> TeacherSubjects { get; set; } = new List<TeacherSubject>();
}
