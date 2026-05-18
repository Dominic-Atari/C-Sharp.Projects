namespace Nile.Managers.Contract.Client.DataContract.V1.User;

public class DeleteUserProfileImageRequest : RequestBase
{
    public Guid UserId { get; set; }
    public Guid ImageId { get; set; }
    public Guid ExternalId { get; set; }
}