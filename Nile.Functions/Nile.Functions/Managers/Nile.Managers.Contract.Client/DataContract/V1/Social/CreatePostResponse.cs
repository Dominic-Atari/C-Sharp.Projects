namespace Nile.Managers.Contract.Client.DataContract.V1.Social;

public class CreatePostResponse : ResponseBase
{
    public required Guid PostId { get; init; }

    public required Guid ExternalId { get; init; }
}