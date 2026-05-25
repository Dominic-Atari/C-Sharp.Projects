using N.LMS.Common.Interface.Context;
using N.LMS.Manager.Admin.Interface.Request;
using N.LMS.Manager.Admin.Interface.Result;

namespace N.LMS.Manager.Admin.Service;

internal sealed partial class AdminManager
{
    private async Task<MeLoadResult> Handle(MeLoadRequest _)
    {
        var context = await ProxyForService<IContextUtility>().GetRequiredContext<UserContext>();

        var accessor = ProxyForService<UserAccessor.IUserAccessor>();
        var accessorResult = (UserAccessor.Result.UserLoadResult)await accessor.Load(
            new UserAccessor.Request.UserLoadRequest { UserId = context.UserId });

        return _Mapper.Map<MeLoadResult>(accessorResult);
    }
}
