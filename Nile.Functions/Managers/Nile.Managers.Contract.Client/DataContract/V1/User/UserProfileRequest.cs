namespace Nile.Managers.Contract.Client.DataContract.V1.User;

public class UserProfileRequest : RequestBase
{
    public required Guid TargetUserId { get; set; }
}