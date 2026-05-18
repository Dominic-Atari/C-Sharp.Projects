namespace Nile.Common.InternalDTOs;

public class CreatePostRequest : FeedPostRequestBase
{
    public Guid UserId { get; set; }

    public string Caption { get; set; } = null!;

    public Guid[] PostIds { get; set; } = null!;

    public string ImageFilename { get; set; } = null!;
}