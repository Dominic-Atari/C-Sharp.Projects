namespace N.LMS.Manager.Learning.Interface.Request;

public sealed record CourseStructureLoadRequest : LearningLoadRequestBase
{
    public Guid CourseId { get; init; }
}
