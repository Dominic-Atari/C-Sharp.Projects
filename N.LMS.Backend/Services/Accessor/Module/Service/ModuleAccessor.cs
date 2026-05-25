using N.LMS.Accessor.Module.Interface;
using N.LMS.Accessor.Module.Interface.Request;
using N.LMS.Common.Interface.Exceptions;
using N.LMS.Common.Interface.Framework;
using N.LMS.Common.Interface.Request;
using N.LMS.Common.Interface.Result;

namespace N.LMS.Accessor.Module.Service;

internal sealed partial class ModuleAccessor : ProxyEnabledServiceBase, IModuleAccessor
{
    public async Task<LoadResultBase> Load(LoadRequestBase request) => request switch
    {
        ModuleLoadRequest r      => await Handle(r),
        ModulesByCourseRequest r => await Handle(r),
        _ => throw new MissingHandlerException(this, request.GetType())
    };

    public async Task<StoreResultBase> Store(StoreRequestBase request) => request switch
    {
        ModuleStoreRequest r => await Handle(r),
        _ => throw new MissingHandlerException(this, request.GetType())
    };

    public async Task<DeleteResultBase> Delete(DeleteRequestBase request) => request switch
    {
        ModuleDeleteRequest r => await Handle(r),
        _ => throw new MissingHandlerException(this, request.GetType())
    };
}
