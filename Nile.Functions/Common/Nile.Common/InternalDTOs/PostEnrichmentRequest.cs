namespace Nile.Common.InternalDTOs;

public class PostEnrichmentRequest : FeedPostRequestBase
{
    public required Guid[] Ids { get; set; }
}