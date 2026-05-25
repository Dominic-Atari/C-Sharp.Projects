using N.LMS.Common.Interface.Framework;
using N.LMS.Common.Interface.Request;
using N.LMS.Common.Interface.Result;

namespace N.LMS.Accessor.Course.Interface;

public interface ICourseAccessor : IComponent
{
    Task<LoadResultBase> Load(LoadRequestBase request);
    Task<StoreResultBase> Store(StoreRequestBase request);
    Task<DeleteResultBase> Delete(DeleteRequestBase request);
}
