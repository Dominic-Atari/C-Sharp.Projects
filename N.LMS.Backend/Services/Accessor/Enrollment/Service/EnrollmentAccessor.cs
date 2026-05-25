using N.LMS.Accessor.Enrollment.Interface;
using N.LMS.Accessor.Enrollment.Interface.Request;
using N.LMS.Common.Interface.Exceptions;
using N.LMS.Common.Interface.Framework;
using N.LMS.Common.Interface.Request;
using N.LMS.Common.Interface.Result;

namespace N.LMS.Accessor.Enrollment.Service;

internal sealed partial class EnrollmentAccessor : ProxyEnabledServiceBase, IEnrollmentAccessor
{
    public async Task<LoadResultBase> Load(LoadRequestBase request) => request switch
    {
        EnrollmentLoadRequest r       => await Handle(r),
        EnrollmentsByCourseRequest r  => await Handle(r),
        EnrollmentsByUserRequest r    => await Handle(r),
        _ => throw new MissingHandlerException(this, request.GetType())
    };

    public async Task<StoreResultBase> Store(StoreRequestBase request) => request switch
    {
        EnrollmentStoreRequest r => await Handle(r),
        _ => throw new MissingHandlerException(this, request.GetType())
    };

    public async Task<DeleteResultBase> Delete(DeleteRequestBase request) => request switch
    {
        EnrollmentDeleteRequest r => await Handle(r),
        _ => throw new MissingHandlerException(this, request.GetType())
    };
}
