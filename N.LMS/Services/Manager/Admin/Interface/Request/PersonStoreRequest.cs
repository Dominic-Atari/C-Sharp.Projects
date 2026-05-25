using N.LMS.Manager.Admin.Interface.Model;

namespace N.LMS.Manager.Admin.Interface.Request;

public sealed record PersonStoreRequest : AdminStoreRequestBase
{
    public Guid? PersonId { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Email { get; init; }
    public string? Password { get; init; }
    public PersonRole Role { get; init; } = PersonRole.Student;
    public Guid? CohortId { get; init; }
}
