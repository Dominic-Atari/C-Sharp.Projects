namespace Nile.Common.DataContracts;

public class CreateUserRequest : StoreUserRequestBase
{
    public string EmailAddress { get; init; } = null!;

    public string ExternalAuthId { get; init; } = null!;
}