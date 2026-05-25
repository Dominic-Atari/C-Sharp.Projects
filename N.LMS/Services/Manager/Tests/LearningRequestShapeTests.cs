using N.LMS.Common.Interface.Request;
using N.LMS.Common.Interface.Result;
using N.LMS.Manager.Learning.Interface.Request;
using N.LMS.Manager.Learning.Interface.Result;
using Xunit;

namespace N.LMS.Manager.Tests.Learning;

public class LearningRequestShapeTests
{
    [Fact]
    public void Load_Requests_Inherit_Manager_Bases()
    {
        Assert.IsAssignableFrom<LoadRequestBase>(new CourseCatalogLoadRequest());
        Assert.IsAssignableFrom<LoadRequestBase>(new MyEnrollmentsLoadRequest());
        Assert.IsAssignableFrom<LoadRequestBase>(new CourseStructureLoadRequest { CourseId = Guid.NewGuid() });
    }

    [Fact]
    public void EnrollInCourse_Inherits_StoreRequestBase() =>
        Assert.IsAssignableFrom<StoreRequestBase>(new EnrollInCourseRequest { CourseId = Guid.NewGuid() });

    [Fact]
    public void Health_Inherits_HealthCheckRequestBase() =>
        Assert.IsAssignableFrom<HealthCheckRequestBase>(new LearningHealthCheckRequest());

    [Fact]
    public void Health_Result_Inherits_HealthCheckResultBase() =>
        Assert.IsAssignableFrom<HealthCheckResultBase>(
            new LearningHealthCheckResult { Healthy = true });
}
