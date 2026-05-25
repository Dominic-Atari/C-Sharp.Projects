using N.LMS.Accessor.System.Interface;
using N.LMS.Accessor.System.Interface.Request;
using N.LMS.Common.Interface.Exceptions;
using N.LMS.Common.Interface.Framework;
using N.LMS.Common.Interface.Request;
using N.LMS.Common.Interface.Result;

namespace N.LMS.Accessor.System.Service;

internal sealed partial class SystemAccessor : ProxyEnabledServiceBase, ISystemAccessor
{
    public async Task<LoadResultBase> Load(LoadRequestBase request) => request switch
    {
        SystemInfoRequest r   => await Handle(r),
        SystemHealthRequest r => await Handle(r),
        _ => throw new MissingHandlerException(this, request.GetType())
    };
}
