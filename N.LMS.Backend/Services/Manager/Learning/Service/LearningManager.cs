using N.LMS.Common.Interface.Exceptions;
using N.LMS.Common.Interface.Framework;
using N.LMS.Common.Interface.Request;
using N.LMS.Common.Interface.Result;
using N.LMS.Manager.Learning.Interface;
using N.LMS.Manager.Learning.Interface.Request;

namespace N.LMS.Manager.Learning.Service;

internal sealed partial class LearningManager : ProxyEnabledServiceBase, ILearningManager
{
    private readonly Mapper _Mapper = new();

    public async Task<HealthCheckResultBase> HealthCheck(HealthCheckRequestBase request) => request switch
    {
        LearningHealthCheckRequest r => await Handle(r),
        _ => throw new MissingHandlerException(this, request.GetType())
    };

    public async Task<LoadResultBase> Load(LoadRequestBase request) => request switch
    {
        CourseCatalogLoadRequest r   => await Handle(r),
        MyEnrollmentsLoadRequest r   => await Handle(r),
        CourseStructureLoadRequest r => await Handle(r),
        _ => throw new MissingHandlerException(this, request.GetType())
    };

    public async Task<StoreResultBase> Store(StoreRequestBase request) => request switch
    {
        EnrollInCourseRequest r => await Handle(r),
        _ => throw new MissingHandlerException(this, request.GetType())
    };
}
