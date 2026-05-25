using N.LMS.Accessor.Lesson.Interface;
using N.LMS.Accessor.Lesson.Interface.Request;
using N.LMS.Common.Interface.Exceptions;
using N.LMS.Common.Interface.Framework;
using N.LMS.Common.Interface.Request;
using N.LMS.Common.Interface.Result;

namespace N.LMS.Accessor.Lesson.Service;

internal sealed partial class LessonAccessor : ProxyEnabledServiceBase, ILessonAccessor
{
    public async Task<LoadResultBase> Load(LoadRequestBase request) => request switch
    {
        LessonLoadRequest r       => await Handle(r),
        LessonsByModuleRequest r  => await Handle(r),
        _ => throw new MissingHandlerException(this, request.GetType())
    };

    public async Task<StoreResultBase> Store(StoreRequestBase request) => request switch
    {
        LessonStoreRequest r => await Handle(r),
        _ => throw new MissingHandlerException(this, request.GetType())
    };

    public async Task<DeleteResultBase> Delete(DeleteRequestBase request) => request switch
    {
        LessonDeleteRequest r => await Handle(r),
        _ => throw new MissingHandlerException(this, request.GetType())
    };
}
