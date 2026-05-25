using N.LMS.Manager.Admin.Interface.Model;

namespace N.LMS.Client.WebApi.Response.Admin;

public sealed record MeLoadResponse : ResponseBase
{
    public Person? Person { get; init; }
}
