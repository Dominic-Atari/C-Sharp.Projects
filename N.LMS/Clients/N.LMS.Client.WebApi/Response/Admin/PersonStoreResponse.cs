using N.LMS.Manager.Admin.Interface.Model;

namespace N.LMS.Client.WebApi.Response.Admin;

public sealed record PersonStoreResponse : ResponseBase
{
    public required Person Person { get; init; }
}
