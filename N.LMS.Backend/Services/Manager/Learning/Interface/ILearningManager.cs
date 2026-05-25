using N.LMS.Common.Interface.Framework;
using N.LMS.Common.Interface.Request;
using N.LMS.Common.Interface.Result;

namespace N.LMS.Manager.Learning.Interface;

public interface ILearningManager : IProxyEnabledSubsystem
{
    Task<HealthCheckResultBase> HealthCheck(HealthCheckRequestBase request);
    Task<LoadResultBase>        Load(LoadRequestBase request);
    Task<StoreResultBase>       Store(StoreRequestBase request);
}
