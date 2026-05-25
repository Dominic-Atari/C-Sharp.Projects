using N.LMS.Accessor.Course.Interface.Model;
using N.LMS.Common.Interface.Request;

namespace N.LMS.Accessor.Course.Interface.Request;

public sealed record CourseStoreRequest : StoreRequestBase
{
    public Guid? CourseId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid InstructorId { get; init; }
    public CourseStatus Status { get; init; } = CourseStatus.Draft;
}
