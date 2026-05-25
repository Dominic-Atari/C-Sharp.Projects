using N.LMS.Common.Interface.Exceptions;
using N.LMS.Common.Interface.Framework;
using N.LMS.Common.Interface.Request;
using N.LMS.Common.Interface.Result;
using N.LMS.Manager.Admin.Interface;
using N.LMS.Manager.Admin.Interface.Request;

namespace N.LMS.Manager.Admin.Service;

internal sealed partial class AdminManager : ProxyEnabledServiceBase, IAdminManager
{
    private readonly Mapper _Mapper = new();

    public async Task<HealthCheckResultBase> HealthCheck(HealthCheckRequestBase request) => request switch
    {
        AdminHealthCheckRequest r => await Handle(r),
        _ => throw new MissingHandlerException(this, request.GetType())
    };

    public async Task<LoadResultBase> Load(LoadRequestBase request) => request switch
    {
        PersonLoadRequest r => await Handle(r),
        MeLoadRequest r     => await Handle(r),
        _ => throw new MissingHandlerException(this, request.GetType())
    };

    public async Task<StoreResultBase> Store(StoreRequestBase request) => request switch
    {
        PersonStoreRequest r => await Handle(r),
        _ => throw new MissingHandlerException(this, request.GetType())
    };

    public async Task<DeleteResultBase> Delete(DeleteRequestBase request) => request switch
    {
        PersonDeleteRequest r => await Handle(r),
        _ => throw new MissingHandlerException(this, request.GetType())
    };
}
