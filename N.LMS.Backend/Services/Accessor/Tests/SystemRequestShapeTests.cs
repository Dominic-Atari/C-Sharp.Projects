using N.LMS.Accessor.System.Interface.Request;
using N.LMS.Common.Interface.Request;
using Xunit;

namespace N.LMS.Accessor.Tests.System;

public class SystemRequestShapeTests
{
    [Fact]
    public void Info_Inherits_LoadRequestBase() =>
        Assert.IsAssignableFrom<LoadRequestBase>(new SystemInfoRequest());

    [Fact]
    public void Health_Inherits_LoadRequestBase() =>
        Assert.IsAssignableFrom<LoadRequestBase>(new SystemHealthRequest());
}
