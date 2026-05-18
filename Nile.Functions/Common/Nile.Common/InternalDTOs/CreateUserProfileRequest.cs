namespace Nile.Common.InternalDTOs;

public class CreateUserProfileRequest : StoreUserRequestBase
{
    public required string FirstName { get; init; }

    public required string LastName { get; init; }

    public string? Location { get; init; }

    public required string Username { get; init; }
}