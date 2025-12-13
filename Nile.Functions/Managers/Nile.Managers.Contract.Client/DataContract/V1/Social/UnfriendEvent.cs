namespace Nile.Managers.Contract.Client.DataContract.V1.Social;

public class UnfriendEvent : RequestBase
{
    public Guid UserId { get; set; }
    public Guid FriendId { get; set; }
}