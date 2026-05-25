using N.LMS.Manager.Admin.Interface.Request;
using N.LMS.Manager.Admin.Interface.Result;

namespace N.LMS.Manager.Admin.Service;

internal sealed partial class AdminManager
{
    private async Task<PersonDeleteResult> Handle(PersonDeleteRequest request)
    {
        var accessor = ProxyForService<UserAccessor.IUserAccessor>();

        var accessorRequest = _Mapper.Map<UserAccessor.Request.UserDeleteRequest>(request);
        var accessorResult = (UserAccessor.Result.UserDeleteResult)await accessor.Delete(accessorRequest);

        return new PersonDeleteResult { PersonId = accessorResult.UserId };
    }
}
