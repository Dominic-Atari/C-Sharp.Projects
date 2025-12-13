namespace Nile.Common.InternalDTOs;

public class UpdatePostExternalIdRequest : FeedPostRequestBase
{
    public Guid PostId { get; init; }
    
    public Guid ExternalId { get; init; }
}