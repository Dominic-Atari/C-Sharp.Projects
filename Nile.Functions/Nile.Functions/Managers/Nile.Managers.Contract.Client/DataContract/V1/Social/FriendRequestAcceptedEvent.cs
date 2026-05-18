namespace Nile.Managers.Contract.Client.DataContract.V1.Social;

public class FriendRequestAcceptedEvent : RequestBase
{
    public Guid UserRelationshipId { get; set; }
}