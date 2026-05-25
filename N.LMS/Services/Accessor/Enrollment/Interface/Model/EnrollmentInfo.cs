namespace N.LMS.Accessor.Enrollment.Interface.Model;

public sealed record EnrollmentInfo
{
    public Guid EnrollmentId { get; init; }
    public Guid UserId { get; init; }
    public string? UserName { get; init; }
    public string? UserEmail { get; init; }
    public Guid CourseId { get; init; }
    public string? CourseTitle { get; init; }
    public EnrollmentRole Role { get; init; }
    public EnrollmentStatus Status { get; init; }
    public DateTime EnrolledUtc { get; init; }
    public DateTime? CompletedUtc { get; init; }
    public decimal ProgressPercent { get; init; }
    public DateTime CreatedUtc { get; init; }
    public DateTime ModifiedUtc { get; init; }
}
