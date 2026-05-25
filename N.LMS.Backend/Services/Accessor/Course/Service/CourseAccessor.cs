using N.LMS.Accessor.Course.Interface;
using N.LMS.Accessor.Course.Interface.Request;
using N.LMS.Common.Interface.Exceptions;
using N.LMS.Common.Interface.Framework;
using N.LMS.Common.Interface.Request;
using N.LMS.Common.Interface.Result;

namespace N.LMS.Accessor.Course.Service;

internal sealed partial class CourseAccessor : ProxyEnabledServiceBase, ICourseAccessor
{
    public async Task<LoadResultBase> Load(LoadRequestBase request) => request switch
    {
        CourseLoadRequest r          => await Handle(r),
        CourseExistsRequest r        => await Handle(r),
        CoursesByInstructorRequest r => await Handle(r),
        _ => throw new MissingHandlerException(this, request.GetType())
    };

    public async Task<StoreResultBase> Store(StoreRequestBase request) => request switch
    {
        CourseStoreRequest r => await Handle(r),
        _ => throw new MissingHandlerException(this, request.GetType())
    };

    public async Task<DeleteResultBase> Delete(DeleteRequestBase request) => request switch
    {
        CourseDeleteRequest r => await Handle(r),
        _ => throw new MissingHandlerException(this, request.GetType())
    };
}
