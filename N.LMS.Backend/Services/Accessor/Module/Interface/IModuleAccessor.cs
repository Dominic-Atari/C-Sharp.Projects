using N.LMS.Common.Interface.Framework;
using N.LMS.Common.Interface.Request;
using N.LMS.Common.Interface.Result;

namespace N.LMS.Accessor.Module.Interface;

public interface IModuleAccessor : IComponent
{
    Task<LoadResultBase> Load(LoadRequestBase request);
    Task<StoreResultBase> Store(StoreRequestBase request);
    Task<DeleteResultBase> Delete(DeleteRequestBase request);
}
