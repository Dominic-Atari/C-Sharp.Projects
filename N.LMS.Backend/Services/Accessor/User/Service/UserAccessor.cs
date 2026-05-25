using N.LMS.Accessor.User.Interface;
using N.LMS.Accessor.User.Interface.Request;
using N.LMS.Common.Interface.Exceptions;
using N.LMS.Common.Interface.Framework;
using N.LMS.Common.Interface.Request;
using N.LMS.Common.Interface.Result;

namespace N.LMS.Accessor.User.Service;

internal sealed partial class UserAccessor : ProxyEnabledServiceBase, IUserAccessor
{
    public async Task<LoadResultBase> Load(LoadRequestBase request) => request switch
    {
        UserLoadRequest r        => await Handle(r),
        UserExistsRequest r      => await Handle(r),
        UserEmailExistsRequest r => await Handle(r),
        _ => throw new MissingHandlerException(this, request.GetType())
    };

    public async Task<StoreResultBase> Store(StoreRequestBase request) => request switch
    {
        UserStoreRequest r => await Handle(r),
        _ => throw new MissingHandlerException(this, request.GetType())
    };

    public async Task<DeleteResultBase> Delete(DeleteRequestBase request) => request switch
    {
        UserDeleteRequest r => await Handle(r),
        _ => throw new MissingHandlerException(this, request.GetType())
    };
}
