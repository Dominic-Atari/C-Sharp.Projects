namespace Nile.Managers.Contract.Client.DataContract.V1.Social;

public class PostEnrichmentRequest : RequestBase
{
    public required Guid[] Ids { get; set; }
}