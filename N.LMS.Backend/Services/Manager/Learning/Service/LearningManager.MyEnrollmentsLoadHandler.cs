using N.LMS.Common.Interface.Context;
using N.LMS.Manager.Learning.Interface.Model;
using N.LMS.Manager.Learning.Interface.Request;
using N.LMS.Manager.Learning.Interface.Result;

namespace N.LMS.Manager.Learning.Service;

internal sealed partial class LearningManager
{
    private async Task<MyEnrollmentsLoadResult> Handle(MyEnrollmentsLoadRequest _)
    {
        var context = await ProxyForService<IContextUtility>().GetRequiredContext<UserContext>();

        var enrollmentAccessor = ProxyForService<EnrollmentAccessor.IEnrollmentAccessor>();
        var accessorResult = (EnrollmentAccessor.Result.EnrollmentListResult)await enrollmentAccessor.Load(
            new EnrollmentAccessor.Request.EnrollmentsByUserRequest { UserId = context.UserId });

        var enrollments = accessorResult.Enrollments.Select(e => _Mapper.Map<MyEnrollment>(e)).ToList();
        return new MyEnrollmentsLoadResult { Enrollments = enrollments };
    }
}
