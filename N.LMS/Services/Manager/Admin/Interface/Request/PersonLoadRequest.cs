namespace N.LMS.Manager.Admin.Interface.Request;

public sealed record PersonLoadRequest : AdminLoadRequestBase
{
    public Guid PersonId { get; init; }
}
