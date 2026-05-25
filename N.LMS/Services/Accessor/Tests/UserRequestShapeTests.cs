using N.LMS.Accessor.User.Interface.Model;
using N.LMS.Accessor.User.Interface.Request;
using N.LMS.Accessor.User.Interface.Result;
using N.LMS.Common.Interface.Request;
using N.LMS.Common.Interface.Result;
using Xunit;

namespace N.LMS.Accessor.Tests.User;

public class UserRequestShapeTests
{
    [Fact]
    public void Load_Variants_Inherit_LoadRequestBase()
    {
        Assert.IsAssignableFrom<LoadRequestBase>(new UserLoadRequest { UserId = Guid.NewGuid() });
        Assert.IsAssignableFrom<LoadRequestBase>(new UserExistsRequest { UserId = Guid.NewGuid() });
        Assert.IsAssignableFrom<LoadRequestBase>(new UserEmailExistsRequest { Email = "a@b.com" });
    }

    [Fact]
    public void Store_Inherits_StoreRequestBase()
    {
        var r = new UserStoreRequest
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@n.lms",
            Password = "secret",
            Role = UserRole.Instructor
        };
        Assert.IsAssignableFrom<StoreRequestBase>(r);
        Assert.Equal(UserRole.Instructor, r.Role);
    }

    [Fact]
    public void Delete_Inherits_DeleteRequestBase()
    {
        Assert.IsAssignableFrom<DeleteRequestBase>(new UserDeleteRequest { UserId = Guid.NewGuid() });
    }

    [Fact]
    public void Result_Default_Is_Successful()
    {
        ResultBase result = new UserExistsResult { Exists = true };
        Assert.True(result.IsSuccessful);
    }
}
