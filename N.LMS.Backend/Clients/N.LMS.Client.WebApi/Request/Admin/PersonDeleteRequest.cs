namespace N.LMS.Client.WebApi.Request.Admin;

public sealed record PersonDeleteRequest : RequestBase
{
    public Guid PersonId { get; init; }
}
