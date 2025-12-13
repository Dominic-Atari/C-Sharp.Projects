namespace Nile.Managers.Contract.Client.DataContract.V1.User;

public class LoginUserProfile
{
    public Guid UserId { get; init; }
    
    public string? Username { get; init; }

    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public string? ImageUrl { get; init; }
}