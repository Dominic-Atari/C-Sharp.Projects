using N.LMS.Common.Interface.Framework;
using N.LMS.Common.Interface.Request;
using N.LMS.Common.Interface.Result;

namespace N.LMS.Manager.Admin.Interface;

public interface IAdminManager : IProxyEnabledSubsystem
{
    Task<HealthCheckResultBase> HealthCheck(HealthCheckRequestBase request);
    Task<LoadResultBase>        Load(LoadRequestBase request);
    Task<StoreResultBase>       Store(StoreRequestBase request);
    Task<DeleteResultBase>      Delete(DeleteRequestBase request);
}
