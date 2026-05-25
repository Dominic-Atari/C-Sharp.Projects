using N.LMS.Manager.Admin.Interface.Request;
using N.LMS.Manager.Admin.Interface.Result;

namespace N.LMS.Manager.Admin.Service;

internal sealed partial class AdminManager
{
    private async Task<PersonStoreResult> Handle(PersonStoreRequest request)
    {
        var accessor = ProxyForService<UserAccessor.IUserAccessor>();

        var accessorRequest = _Mapper.Map<UserAccessor.Request.UserStoreRequest>(request);
        var accessorResult = (UserAccessor.Result.UserStoreResult)await accessor.Store(accessorRequest);

        return _Mapper.Map<PersonStoreResult>(accessorResult);
    }
}
