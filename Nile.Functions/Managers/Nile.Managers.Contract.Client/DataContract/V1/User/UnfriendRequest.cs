namespace Nile.Managers.Contract.Client.DataContract.V1.User;

public class UnfriendRequest : RequestBase
{
    public Guid TargetUserId { get; set; }
}