namespace N.LMS.Manager.Learning.Interface.Request;

public sealed record EnrollInCourseRequest : LearningStoreRequestBase
{
    public Guid CourseId { get; init; }
}
