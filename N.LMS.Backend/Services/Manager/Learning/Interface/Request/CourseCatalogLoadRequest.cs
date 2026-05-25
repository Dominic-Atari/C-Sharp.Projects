namespace N.LMS.Manager.Learning.Interface.Request;

public sealed record CourseCatalogLoadRequest : LearningLoadRequestBase
{
    public Guid? InstructorId { get; init; }
}
