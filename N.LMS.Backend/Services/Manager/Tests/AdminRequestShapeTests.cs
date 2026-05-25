using N.LMS.Common.Interface.Request;
using N.LMS.Common.Interface.Result;
using N.LMS.Manager.Admin.Interface.Model;
using N.LMS.Manager.Admin.Interface.Request;
using N.LMS.Manager.Admin.Interface.Result;
using Xunit;

namespace N.LMS.Manager.Tests.Admin;

public class AdminRequestShapeTests
{
    [Fact]
    public void PersonRequests_Inherit_Manager_Bases()
    {
        Assert.IsAssignableFrom<LoadRequestBase>(new PersonLoadRequest { PersonId = Guid.NewGuid() });
        Assert.IsAssignableFrom<DeleteRequestBase>(new PersonDeleteRequest { PersonId = Guid.NewGuid() });
        Assert.IsAssignableFrom<StoreRequestBase>(
            new PersonStoreRequest { FirstName = "A", LastName = "B", Email = "a@b.com" });
    }

    [Fact]
    public void MeLoad_Inherits_LoadRequestBase() =>
        Assert.IsAssignableFrom<LoadRequestBase>(new MeLoadRequest());

    [Fact]
    public void HealthCheck_Result_Defaults_To_UtcNow()
    {
        var r = new AdminHealthCheckResult { Healthy = true };
        Assert.True(r.CheckedAtUtc <= DateTime.UtcNow);
        Assert.IsAssignableFrom<HealthCheckResultBase>(r);
    }

    [Fact]
    public void PersonStore_Defaults_To_Student()
    {
        var r = new PersonStoreRequest { FirstName = "A", LastName = "B", Email = "a@b.com" };
        Assert.Equal(PersonRole.Student, r.Role);
    }
}
