namespace Nile.Managers.Contract.Client.DataContract.V1.User;

public class UserSettings
{
    public bool NotifyOnFriendRequestReceived { get; set; }

    public bool NotifyOnFriendRequestApproved { get; set; }
    
    public bool NotifyOnNewPost { get; set; }
    public bool NotifyOnNewComment { get; set; }
    public bool NotifyOnNewLike { get; set; }
    public bool NotifyOnNewFollow { get; set; }
    public bool NotifyOnNewMessage { get; set; }
    public bool NotifyOnNewNotification { get; set; }
    public bool NotifyOnNewMention { get; set; }
    public bool NotifyOnNewPrivateMessage { get; set; }
    public bool NotifyOnNewGroupMessage { get; set; }
    public bool NotifyOnNewAccountVerification { get; set; }
    public bool NotifyOnNewAccountApproval { get; set; }
    public bool NotifyOnNewAccountDeletion { get; set; }
    public bool NotifyOnNewAccountDeactivation { get; set; }
    public bool NotifyOnNewAccountLockout { get; set; }
    public bool NotifyOnNewAccountUnlock { get; set; }
    
}