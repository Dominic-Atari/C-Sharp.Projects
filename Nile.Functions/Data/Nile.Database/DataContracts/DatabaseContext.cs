using Microsoft.EntityFrameworkCore;
using Nile.Common;
using Nile.Common.Extensions;
using Nile.Database.Entities;

namespace Nile.Database.DataContracts;

/// <summary>
/// Main database context for the Nile application.
/// </summary>
public class DatabaseContext : DatabaseContextBase<DatabaseContext>
{
    public DatabaseContext(DbContextOptions<DatabaseContext> options, IConfigUtility config)
        : base(options, config)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Map tables that differ in DB scripts (singular names)
        modelBuilder.Entity<Role>().ToTable("Role");
        modelBuilder.Entity<UserRole>().ToTable("UserRole");

        // Configure one-to-one User <-> UserProfile
        modelBuilder.Entity<User>()
            .HasOne(u => u.Profile)
            .WithOne(p => p.User)
            .HasForeignKey<UserProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure one-to-one User <-> Passwords (User is principal, Passwords is dependent)
        modelBuilder.Entity<User>()
            .HasOne(u => u.Password)
            .WithOne(p => p.User)
            .HasForeignKey<Passwords>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relationships with Cascade Delete Strategy

        // User -> Roles (Restrict: Don't delete user if they have roles)
        modelBuilder.Entity<UserRole>()
       .HasKey(ur => new { ur.UserId, ur.RoleId });


        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // UserRole -> Role (Restrict: Don't delete role if it's assigned to users)
        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
        // User -> Posts (Restrict: Don't delete user if they have posts)
        modelBuilder.Entity<Post>()
            .HasOne(p => p.User)
            .WithMany(u => u.Posts)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Post -> Comments (Cascade: Delete all comments when post is deleted)
        modelBuilder.Entity<Comment>()
            .HasOne(c => c.Post)
            .WithMany(p => p.Comments)
            .HasForeignKey(c => c.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        // User -> Comments (Restrict: Don't delete user if they have comments)
        modelBuilder.Entity<Comment>()
            .HasOne(c => c.User)
            .WithMany(u => u.Comments)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Comment -> Replies (Cascade: Delete all replies when parent comment is deleted)
        modelBuilder.Entity<Comment>()
            .HasOne(c => c.ParentComment)
            .WithMany(c => c.Replies)
            .HasForeignKey(c => c.ParentCommentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Post -> Likes (Cascade: Delete all likes when post is deleted)
        modelBuilder.Entity<Like>()
            .HasOne(l => l.Post)
            .WithMany(p => p.Likes)
            .HasForeignKey(l => l.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        // Make the PostId FK optional to avoid EF warning when Post has a global query filter
        modelBuilder.Entity<Like>()
            .Property(l => l.PostId)
            .IsRequired(false);

        // User -> Likes (Restrict: Don't delete user if they have likes)
        modelBuilder.Entity<Like>()
            .HasOne(l => l.User)
            .WithMany(u => u.Likes)
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // UserRelationship -> Follower (Restrict to avoid accidental hard delete cascades)
        modelBuilder.Entity<UserRelationship>()
            .HasOne(ur => ur.Follower)
            .WithMany(u => u.Following)
            .HasForeignKey(ur => ur.FollowerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // UserRelationship -> Following (Restrict: Prevent deletion cycles)
        modelBuilder.Entity<UserRelationship>()
            .HasOne(ur => ur.Following)
            .WithMany(u => u.Followers)
            .HasForeignKey(ur => ur.FollowingUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserRelationship>()
            .HasKey(ur => ur.RelationshipId);

        // Token tables - define primary keys for in-memory provider
        modelBuilder.Entity<EmailConfirmations>()
            .HasKey(ec => ec.UserId);

        modelBuilder.Entity<ResetPasswordTokens>()
            .HasKey(rp => rp.UserId);

        modelBuilder.Entity<Passwords>()
            .HasKey(pw => pw.UserId);

        // Notification -> User (Restrict to align with soft-delete-only)
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Notification -> Actor (SetNull: Keep notification but clear actor when actor user is deleted)
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.Actor)
            .WithMany(u => u.TriggeredNotifications)
            .HasForeignKey(n => n.ActorUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Global Query Filters - Automatically filter out soft-deleted records where present
        // Note: Users no longer implement soft delete in minimal schema
        modelBuilder.Entity<Post>()
            .HasQueryFilter(x => !x.IsDeleted);

        modelBuilder.Entity<Comment>()
            .HasQueryFilter(x => !x.IsDeleted);

        // School ownership: one head teacher owns one school
        modelBuilder.Entity<School>()
            .HasOne(s => s.HeadTeacher)
            .WithOne(u => u.School)
            .HasForeignKey<School>(s => s.HeadTeacherUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Memberships: many-to-many via join entity
        modelBuilder.Entity<SchoolMembership>()
            .HasIndex(sm => new { sm.SchoolId, sm.UserId })
            .IsUnique();

        modelBuilder.Entity<SchoolMembership>()
            .HasOne(sm => sm.School)
            .WithMany(s => s.Memberships)
            .HasForeignKey(sm => sm.SchoolId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SchoolMembership>()
            .HasOne(sm => sm.User)
            .WithMany(u => u.SchoolMemberships)
            .HasForeignKey(sm => sm.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Subjects and teacher assignments
        modelBuilder.Entity<Subject>()
            .HasIndex(s => new { s.SchoolId, s.Name, s.Stage })
            .IsUnique();
        // optional SubLevel relationship for Subjects
        modelBuilder.Entity<Subject>()
            .HasOne<SubLevel>(s => s.SubLevel)
            .WithMany()
            .HasForeignKey("SubLevelId")
            .OnDelete(DeleteBehavior.SetNull);

        // Topics
        modelBuilder.Entity<Topic>()
            .HasIndex(t => new { t.SubjectId });

        // Automatically filter out soft-deleted topics
        modelBuilder.Entity<Topic>()
            .HasQueryFilter(t => !t.IsDeleted);

        modelBuilder.Entity<Topic>()
            .HasOne(t => t.Subject)
            .WithMany()
            .HasForeignKey(t => t.SubjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // SubTopics
        modelBuilder.Entity<SubTopic>()
            .HasIndex(st => new { st.TopicId, st.Name })
            .IsUnique();

        modelBuilder.Entity<SubTopic>()
            .HasQueryFilter(st => !st.IsDeleted);

        modelBuilder.Entity<SubTopic>()
            .HasOne(st => st.Topic)
            .WithMany(t => t.SubTopics)
            .HasForeignKey(st => st.TopicId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TeacherSubject>()
            .HasIndex(ts => new { ts.UserId, ts.SchoolId, ts.SubjectId })
            .IsUnique();

        // SubLevels (grades within a Stage)
        modelBuilder.Entity<SubLevel>()
            .HasIndex(sl => new { sl.StageId, sl.Name })
            .IsUnique();

        modelBuilder.Entity<SubLevel>()
            .HasOne(sl => sl.Stage)
            .WithMany()
            .HasForeignKey(sl => sl.StageId)
            .OnDelete(DeleteBehavior.Cascade);

        // Stages/Levels
        modelBuilder.Entity<Stage>()
            .HasIndex(s => new { s.SchoolId, s.Name })
            .IsUnique();

        modelBuilder.Entity<Stage>()
            .HasOne(s => s.School)
            .WithMany()
            .HasForeignKey(s => s.SchoolId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TeacherSubject>()
            .HasOne(ts => ts.Teacher)
            .WithMany(u => u.TeacherSubjects)
            .HasForeignKey(ts => ts.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TeacherSubject>()
            .HasOne(ts => ts.School)
            .WithMany()
            .HasForeignKey(ts => ts.SchoolId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TeacherSubject>()
            .HasOne(ts => ts.Subject)
            .WithMany(s => s.TeacherSubjects)
            .HasForeignKey(ts => ts.SubjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // Courses and lessons
        modelBuilder.Entity<Course>()
            .HasOne(c => c.School)
            .WithMany(s => s.Courses)
            .HasForeignKey(c => c.SchoolId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Course>()
            .HasOne(c => c.Subject)
            .WithMany(s => s.Courses)
            .HasForeignKey(c => c.SubjectId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Lesson>()
            .HasOne(l => l.Course)
            .WithMany(c => c.Lessons)
            .HasForeignKey(l => l.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Story -> SubTopic (optional, clear when subtopic deleted)
        modelBuilder.Entity<Story>()
            .HasOne(s => s.SubTopic)
            .WithMany()
            .HasForeignKey("SubTopicId")
            .OnDelete(DeleteBehavior.SetNull);

        // Seed LMS roles (idempotent with migrations)
        var headTeacherRoleId = Guid.Parse("4f6e2ad7-8bc0-4b3f-9d4c-4d6c1c6b0f01");
        var studentRoleId = Guid.Parse("b8d9f9e3-7a3e-4c1b-9d7a-2f8d2f0c9e13");
        var teacherRoleId = Guid.Parse("2c0a9db6-6f45-4d29-9e11-3e6f0b1e5f22");

        modelBuilder.Entity<Role>().HasData(
            new Role { RoleId = headTeacherRoleId, RoleName = "HeadTeacher" },
            new Role { RoleId = studentRoleId, RoleName = "Student" },
            new Role { RoleId = teacherRoleId, RoleName = "Teacher" }
        );
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Like> Likes => Set<Like>();
    public DbSet<UserRelationship> UserRelationships => Set<UserRelationship>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<School> Schools => Set<School>();
    public DbSet<SchoolMembership> SchoolMemberships => Set<SchoolMembership>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<TeacherSubject> TeacherSubjects => Set<TeacherSubject>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<SubTopic> SubTopics => Set<SubTopic>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<Stage> Stages => Set<Stage>();
    public DbSet<SubLevel> SubLevels => Set<SubLevel>();
    public DbSet<Passwords> Passwords => Set<Passwords>();
    public DbSet<EmailConfirmations> EmailConfirmations => Set<EmailConfirmations>();
    public DbSet<ResetPasswordTokens> ResetPasswordTokens => Set<ResetPasswordTokens>();
}
