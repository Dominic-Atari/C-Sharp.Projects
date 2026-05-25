using Microsoft.EntityFrameworkCore;

namespace N.LMS.Database.Interface.Model;

public class NileDbContext : DbContext
{
    public NileDbContext(DbContextOptions<NileDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Cohort> Cohorts => Set<Cohort>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Module> Modules => Set<Module>();
    public DbSet<Lesson> Lessons => Set<Lesson>();

    public override int SaveChanges()
    {
        var changes = base.SaveChanges();
        ChangeTracker.Clear();
        return changes;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var changes = await base.SaveChangesAsync(cancellationToken);
        ChangeTracker.Clear();
        return changes;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<User>()
            .HasOne(u => u.Cohort)
            .WithMany(c => c.Members)
            .HasForeignKey(u => u.CohortId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Course>()
            .HasOne(c => c.Instructor)
            .WithMany()
            .HasForeignKey(c => c.InstructorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Enrollment>()
            .HasOne(e => e.User)
            .WithMany(u => u.Enrollments)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Enrollment>()
            .HasOne(e => e.Course)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Module>()
            .HasOne(m => m.Course)
            .WithMany(c => c.Modules)
            .HasForeignKey(m => m.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Lesson>()
            .HasOne(l => l.Module)
            .WithMany(m => m.Lessons)
            .HasForeignKey(l => l.ModuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Enrollment>()
            .Property(e => e.ProgressPercent)
            .HasPrecision(5, 2);
    }
}
