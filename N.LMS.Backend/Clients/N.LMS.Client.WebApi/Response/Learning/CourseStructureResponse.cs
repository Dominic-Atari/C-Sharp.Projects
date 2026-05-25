using N.LMS.Manager.Learning.Interface.Model;

namespace N.LMS.Client.WebApi.Response.Learning;

public sealed record CourseStructureResponse : ResponseBase
{
    public CourseStructure? Structure { get; init; }
}
