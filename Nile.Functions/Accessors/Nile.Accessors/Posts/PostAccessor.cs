using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nile.Accessors.DataContracts;
using Nile.Database.DataContracts;
using Nile.Utilities.AzureSdk;

namespace Nile.Accessors.Posts;

internal class PostAccessor : AccessorBase, IPostAccessor
{
    private readonly IBlobStorageUtility _blobStorageUtility;
    private readonly IDateUtility _dateUtility;
    private readonly DatabaseContext _db;
    private readonly IConfigurationProvider _mapperConfig;

    public PostAccessor(
        ILogger<PostAccessor> logger,
        IDateUtility dateUtility,
        DatabaseContext db,
        IBlobStorageUtility blobStorageUtility,
        IConfigurationProvider mapperConfig) : base(logger)
    {
        _dateUtility = dateUtility;
        _db = db;
        _blobStorageUtility = blobStorageUtility;
        _mapperConfig = mapperConfig;
    }

    public async Task<DTO.FeedPostResponseBase> Get(DTO.FeedPostRequestBase request)
    {
        return request switch
        {
            DTO.PostRequest req => await PostData(req),
            _ => throw new NotImplementedException(
                $"{nameof(Get)} not implemented for '{request.GetType().Name}' (yet!).")
        };
    }

    private async Task<DTO.FeedPostResponseBase> PostData(DTO.PostRequest req)
    {
        var post = await _db.Posts
            .Where(p => p.PostId == req.PostId)
            .ProjectTo<PostData>(_mapperConfig)
            .SingleOrDefaultAsync();

        if (post == null)
        {
            throw new KeyNotFoundException($"Post with id '{req.PostId}' not found.");
        }

        return new DTO.PostResponse
        {
            UserId = post.UserId,
            ExternalId = post.ExternalId,
        };
    }



    public async Task<DTO.FeedPostResponseBase> Store(DTO.FeedPostRequestBase request)
    {
        return request switch
        {
            DTO.CreatePostRequest req => await CreatePost(req),
            DTO.UpdatePostExternalIdRequest req => await UpdatePostExternalId(req),
            _ => throw new NotImplementedException(
                $"{nameof(Store)} not implemented for '{request.GetType().Name}' (yet!).")
        };
    }

    private async Task<DTO.FeedPostResponseBase> CreatePost(DTO.CreatePostRequest req)
    {
        // Validate FK upfront to surface a friendly error instead of an FK violation
        var userExists = await _db.Users.AnyAsync(u => u.Id == req.UserId);
        if (!userExists)
        {
            throw new KeyNotFoundException($"User '{req.UserId}' not found");
        }

        // Minimal MVP mapping into EF.Post
        var post = new Database.Entities.Post
        {
            PostId = Guid.NewGuid(),
            UserId = req.UserId,
            Title = null,
            Content = req.Caption,
            ImageUrl = string.IsNullOrWhiteSpace(req.ImageFilename) ? null : req.ImageFilename,
            PostType = string.IsNullOrWhiteSpace(req.ImageFilename) ? "Text" : "Image",
            LikesCount = 0,
            CommentsCount = 0,
            SharesCount = 0,
            CreatedAt = _dateUtility.UtcNow,
            UpdatedAt = null,
            IsDeleted = false,
            DeletedAt = null
        };

        await _db.Posts.AddAsync(post);
        await _db.SaveChangesAsync();

        return new DTO.CreatePostResponse
        {
            PostId = post.PostId
        };
    }

    // Strongly-typed convenience overload required by IPostAccessor
    public async Task<DTO.CreatePostResponse> Store(DTO.CreatePostRequest request)
    {
        var response = await CreatePost(request);
        return (DTO.CreatePostResponse)response;
    }

    private async Task<DTO.FeedPostResponseBase> UpdatePostExternalId(DTO.UpdatePostExternalIdRequest req)
    {
        // Schema currently has no ExternalId column on Posts. For now, validate existence and
        // return a PostResponse with the provided ExternalId echoed back for upstream use.
        var exists = await _db.Posts.AnyAsync(p => p.PostId == req.PostId);
        if (!exists)
        {
            throw new KeyNotFoundException($"Post with id '{req.PostId}' not found.");
        }

        // Echo back PostId owner/user and the external id provided by feed utility
        var post = await _db.Posts.Where(p => p.PostId == req.PostId)
            .Select(p => new { p.UserId })
            .SingleAsync();

        return new DTO.PostResponse
        {
            UserId = post.UserId,
            ExternalId = req.ExternalId
        };
    }
}