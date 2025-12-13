using Nile.Common.InternalDTOs;

namespace Nile.Accessors.Posts;

public interface IPostAccessor
{
    Task<FeedPostResponseBase> Get(FeedPostRequestBase request);
    
    Task<FeedPostResponseBase> Store(FeedPostRequestBase request);

    // Strongly-typed convenience overloads used by higher layers
    Task<CreatePostResponse> Store(CreatePostRequest request);
}