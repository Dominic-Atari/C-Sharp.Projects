namespace N.LMS.Manager.Admin.Interface.Model;

public sealed record Person
{
    public Guid PersonId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string Email { get; init; } = string.Empty;
    public PersonRole Role { get; init; }
    public Guid? CohortId { get; init; }
    public string? CohortName { get; init; }
    public DateTime CreatedUtc { get; init; }
    public DateTime ModifiedUtc { get; init; }
}
