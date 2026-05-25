using N.LMS.Manager.Admin.Interface.Request;
using N.LMS.Manager.Admin.Interface.Result;

namespace N.LMS.Manager.Admin.Service;

internal sealed partial class AdminManager
{
    private async Task<PersonLoadResult> Handle(PersonLoadRequest request)
    {
        var accessor = ProxyForService<UserAccessor.IUserAccessor>();

        var accessorRequest = _Mapper.Map<UserAccessor.Request.UserLoadRequest>(request);
        var accessorResult = (UserAccessor.Result.UserLoadResult)await accessor.Load(accessorRequest);

        return _Mapper.Map<PersonLoadResult>(accessorResult);
    }
}
