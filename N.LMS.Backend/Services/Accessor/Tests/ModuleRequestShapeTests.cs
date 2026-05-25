using N.LMS.Accessor.Module.Interface.Request;
using N.LMS.Common.Interface.Request;
using Xunit;

namespace N.LMS.Accessor.Tests.Module;

public class ModuleRequestShapeTests
{
    [Fact]
    public void Load_Variants_Inherit_LoadRequestBase()
    {
        Assert.IsAssignableFrom<LoadRequestBase>(new ModuleLoadRequest { ModuleId = Guid.NewGuid() });
        Assert.IsAssignableFrom<LoadRequestBase>(new ModulesByCourseRequest { CourseId = Guid.NewGuid() });
    }

    [Fact]
    public void Store_Inherits_StoreRequestBase()
    {
        Assert.IsAssignableFrom<StoreRequestBase>(
            new ModuleStoreRequest { CourseId = Guid.NewGuid(), Title = "M1" });
    }

    [Fact]
    public void Delete_Inherits_DeleteRequestBase()
    {
        Assert.IsAssignableFrom<DeleteRequestBase>(new ModuleDeleteRequest { ModuleId = Guid.NewGuid() });
    }
}
