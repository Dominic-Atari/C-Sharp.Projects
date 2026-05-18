namespace Nile.Managers.Contract.Client.DataContract.V1.Social;

public class PostEnrichmentResponse : ResponseBase
{
    public required PostModel[] Posts { get; init; }
}