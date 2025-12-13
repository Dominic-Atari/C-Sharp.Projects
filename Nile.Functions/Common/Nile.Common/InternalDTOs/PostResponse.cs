namespace Nile.Common.InternalDTOs;

public class PostResponse : FeedPostResponseBase
{
    public required Guid UserId { get; init; }
    
    public required Guid ExternalId { get; init; }
}