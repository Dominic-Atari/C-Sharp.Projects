using N.LMS.Accessor.User.Interface.Model;
using N.LMS.Common.Interface.Request;

namespace N.LMS.Accessor.User.Interface.Request;

public sealed record UserStoreRequest : StoreRequestBase
{
    public Guid? UserId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Password { get; init; }
    public UserRole Role { get; init; } = UserRole.Student;
    public Guid? CohortId { get; init; }
}
