namespace Nile.Common.InternalDTOs;

public class DeleteUserProfileImageRequest : UserRequestBase
{
    public Guid UserId { get; set; }
    public Guid ImageId { get; set; }
    public Guid ExternalId { get; set; }
}
