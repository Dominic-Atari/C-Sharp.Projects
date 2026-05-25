using N.LMS.Accessor.Course.Interface.Model;
using N.LMS.Accessor.Course.Interface.Request;
using N.LMS.Common.Interface.Request;
using Xunit;

namespace N.LMS.Accessor.Tests.Course;

public class CourseRequestShapeTests
{
    [Fact]
    public void Load_Variants_Inherit_LoadRequestBase()
    {
        Assert.IsAssignableFrom<LoadRequestBase>(new CourseLoadRequest { CourseId = Guid.NewGuid() });
        Assert.IsAssignableFrom<LoadRequestBase>(new CourseExistsRequest { CourseId = Guid.NewGuid() });
        Assert.IsAssignableFrom<LoadRequestBase>(new CoursesByInstructorRequest { InstructorId = Guid.NewGuid() });
    }

    [Fact]
    public void Store_Inherits_StoreRequestBase_And_Defaults_To_Draft()
    {
        var r = new CourseStoreRequest { Title = "Intro to Pomelo", InstructorId = Guid.NewGuid() };
        Assert.IsAssignableFrom<StoreRequestBase>(r);
        Assert.Equal(CourseStatus.Draft, r.Status);
    }

    [Fact]
    public void Delete_Inherits_DeleteRequestBase()
    {
        Assert.IsAssignableFrom<DeleteRequestBase>(new CourseDeleteRequest { CourseId = Guid.NewGuid() });
    }
}
