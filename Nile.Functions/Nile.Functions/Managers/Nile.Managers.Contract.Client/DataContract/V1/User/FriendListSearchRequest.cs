namespace Nile.Managers.Contract.Client.DataContract.V1.User;

public class FriendListSearchRequest : StoreUserRequestBase
{
    public Guid? UserId { get; init; }

    public string? SearchTerm { get; init; }
}