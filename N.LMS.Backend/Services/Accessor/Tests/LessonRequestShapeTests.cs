using N.LMS.Accessor.Lesson.Interface.Model;
using N.LMS.Accessor.Lesson.Interface.Request;
using N.LMS.Common.Interface.Request;
using Xunit;

namespace N.LMS.Accessor.Tests.Lesson;

public class LessonRequestShapeTests
{
    [Fact]
    public void Load_Variants_Inherit_LoadRequestBase()
    {
        Assert.IsAssignableFrom<LoadRequestBase>(new LessonLoadRequest { LessonId = Guid.NewGuid() });
        Assert.IsAssignableFrom<LoadRequestBase>(new LessonsByModuleRequest { ModuleId = Guid.NewGuid() });
    }

    [Fact]
    public void Store_Defaults_To_Article()
    {
        var r = new LessonStoreRequest { ModuleId = Guid.NewGuid(), Title = "L1" };
        Assert.Equal(LessonType.Article, r.Type);
    }

    [Fact]
    public void Delete_Inherits_DeleteRequestBase()
    {
        Assert.IsAssignableFrom<DeleteRequestBase>(new LessonDeleteRequest { LessonId = Guid.NewGuid() });
    }
}
