namespace Nile.Managers.Contract.Client.DataContract.V1.User;

public class UserContextResponse : ResponseBase
{
    public LoginUserProfile Profile { get; init; } = null!;

    public UserSettings Settings { get; init; } = null!;
    
    //public LoginAccount Account { get; init; } = null!;
}
