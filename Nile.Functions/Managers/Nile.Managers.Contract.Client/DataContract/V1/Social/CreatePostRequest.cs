using System.ComponentModel.DataAnnotations;

namespace Nile.Managers.Contract.Client.DataContract.V1.Social;

public class CreatePostRequest : RequestBase
{
    [Required, MaxLength(2000)] 
    public required string Caption { get; init; }

    [MaxLength(20)]
    public required Guid[] PostId { get; init; }

    [Required]
    public required string ImageFilename { get; init; }
}