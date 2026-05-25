namespace N.LMS.Manager.Admin.Interface.Request;

public sealed record PersonDeleteRequest : AdminDeleteRequestBase
{
    public Guid PersonId { get; init; }
}
