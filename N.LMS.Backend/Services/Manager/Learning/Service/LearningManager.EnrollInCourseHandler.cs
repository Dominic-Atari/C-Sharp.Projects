using N.LMS.Common.Interface.Context;
using N.LMS.Manager.Learning.Interface.Model;
using N.LMS.Manager.Learning.Interface.Request;
using N.LMS.Manager.Learning.Interface.Result;

namespace N.LMS.Manager.Learning.Service;

internal sealed partial class LearningManager
{
    private async Task<EnrollInCourseResult> Handle(EnrollInCourseRequest request)
    {
        var context = await ProxyForService<IContextUtility>().GetRequiredContext<UserContext>();

        var enrollmentAccessor = ProxyForService<EnrollmentAccessor.IEnrollmentAccessor>();
        var accessorResult = (EnrollmentAccessor.Result.EnrollmentStoreResult)await enrollmentAccessor.Store(
            new EnrollmentAccessor.Request.EnrollmentStoreRequest
            {
                UserId = context.UserId,
                CourseId = request.CourseId,
                Role = EnrollmentAccessor.Model.EnrollmentRole.Student,
                Status = EnrollmentAccessor.Model.EnrollmentStatus.Active
            });

        var info = accessorResult.Enrollment
                   ?? throw new InvalidOperationException("Enrollment accessor returned no enrollment.");

        return new EnrollInCourseResult { Enrollment = _Mapper.Map<MyEnrollment>(info) };
    }
}
