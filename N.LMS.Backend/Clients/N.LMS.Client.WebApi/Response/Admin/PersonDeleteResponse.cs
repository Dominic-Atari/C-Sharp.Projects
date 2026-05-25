namespace N.LMS.Client.WebApi.Response.Admin;

public sealed record PersonDeleteResponse : ResponseBase
{
    public Guid PersonId { get; init; }
}
