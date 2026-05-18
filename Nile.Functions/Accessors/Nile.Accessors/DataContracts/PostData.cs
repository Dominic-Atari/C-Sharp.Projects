namespace Nile.Accessors.DataContracts;

internal class PostData
{
    public required Guid UserId { get; init; }
    
    public required Guid ExternalId { get; init; }
}