namespace N.LMS.Manager.Learning.Interface.Model;

public sealed record MyEnrollment
{
    public Guid EnrollmentId { get; init; }
    public Guid CourseId { get; init; }
    public string CourseTitle { get; init; } = string.Empty;
    public decimal ProgressPercent { get; init; }
    public DateTime EnrolledUtc { get; init; }
    public DateTime? CompletedUtc { get; init; }
    public string Status { get; init; } = string.Empty;
}
