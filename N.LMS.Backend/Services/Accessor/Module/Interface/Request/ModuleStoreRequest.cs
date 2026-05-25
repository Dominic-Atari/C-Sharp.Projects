using N.LMS.Common.Interface.Request;

namespace N.LMS.Accessor.Module.Interface.Request;

public sealed record ModuleStoreRequest : StoreRequestBase
{
    public Guid? ModuleId { get; init; }
    public Guid CourseId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int? SortOrder { get; init; }
}
