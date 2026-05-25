using N.LMS.Common.Interface.Framework;
using N.LMS.Common.Interface.Request;
using N.LMS.Common.Interface.Result;

namespace N.LMS.Accessor.System.Interface;

public interface ISystemAccessor : IComponent
{
    Task<LoadResultBase> Load(LoadRequestBase request);
}
