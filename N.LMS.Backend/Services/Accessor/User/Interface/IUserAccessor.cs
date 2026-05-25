using N.LMS.Common.Interface.Framework;
using N.LMS.Common.Interface.Request;
using N.LMS.Common.Interface.Result;

namespace N.LMS.Accessor.User.Interface;

public interface IUserAccessor : IComponent
{
    Task<LoadResultBase> Load(LoadRequestBase request);
    Task<StoreResultBase> Store(StoreRequestBase request);
    Task<DeleteResultBase> Delete(DeleteRequestBase request);
}
