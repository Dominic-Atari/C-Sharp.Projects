using N.LMS.Common.Interface.Context;

namespace N.LMS.Client.WebApi.Extension;

public static class HttpContextExtensions
{
    public static WebContext ToWebContext(this HttpContext httpContext) =>
        new() { Claims = httpContext.User.Claims.ToArray() };
}
