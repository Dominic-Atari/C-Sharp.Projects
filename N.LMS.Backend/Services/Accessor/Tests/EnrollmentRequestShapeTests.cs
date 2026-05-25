using N.LMS.Accessor.Enrollment.Interface.Model;
using N.LMS.Accessor.Enrollment.Interface.Request;
using N.LMS.Common.Interface.Request;
using Xunit;

namespace N.LMS.Accessor.Tests.Enrollment;

public class EnrollmentRequestShapeTests
{
    [Fact]
    public void Load_Variants_Inherit_LoadRequestBase()
    {
        Assert.IsAssignableFrom<LoadRequestBase>(new EnrollmentLoadRequest { EnrollmentId = Guid.NewGuid() });
        Assert.IsAssignableFrom<LoadRequestBase>(new EnrollmentsByCourseRequest { CourseId = Guid.NewGuid() });
        Assert.IsAssignableFrom<LoadRequestBase>(new EnrollmentsByUserRequest { UserId = Guid.NewGuid() });
    }

    [Fact]
    public void Store_Defaults_To_Active_Student()
    {
        var r = new EnrollmentStoreRequest { UserId = Guid.NewGuid(), CourseId = Guid.NewGuid() };
        Assert.Equal(EnrollmentRole.Student, r.Role);
        Assert.Equal(EnrollmentStatus.Active, r.Status);
    }
}
