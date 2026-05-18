namespace Nile.Common.InternalDTOs;

public class CreateUserRequest : UserRequestBase
{
    public string EmailAddress { get; init; } = null!;

    public string ExternalAuthId { get; init; } = null!;
}