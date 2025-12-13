using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Nile.Database.DataContracts;
using Nile.Database.Entities;
using Nile.Functions.Functions.Models;
using Nile.Functions.Functions.Infrastructure;

namespace Nile.Functions.Functions
{
    public class PostsFunction
    {
        private readonly DatabaseContext _db;
        private readonly IConfiguration _config;

        public PostsFunction(DatabaseContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        [Function("CreatePost")]
        public async Task<HttpResponseData> CreatePost(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "posts")] HttpRequestData req)
        {
            {
                var unauthorized = await AuthGuard.Authorize(req, _config);
                if (unauthorized != null) return unauthorized;
            }

            var payload = await req.ReadFromJsonAsync<CreatePostRequest>();
            if (payload == null)
            {
                return await Error(req, HttpStatusCode.BadRequest, "Invalid JSON body.");
            }
            if (payload.UserId == Guid.Empty || string.IsNullOrWhiteSpace(payload.Caption))
            {
                return await Error(req, HttpStatusCode.BadRequest, "userId and caption are required.");
            }

            var userExists = await _db.Users.AnyAsync(u => u.Id == payload.UserId);
            if (!userExists)
            {
                return await Error(req, HttpStatusCode.NotFound, "user not found");
            }

            var now = DateTime.UtcNow;
            var post = new Post
            {
                PostId = Guid.NewGuid(),
                UserId = payload.UserId,
                Content = payload.Caption.Trim(),
                ImageUrl = string.IsNullOrWhiteSpace(payload.ImageUrl) ? null : payload.ImageUrl.Trim(),
                PostType = string.IsNullOrWhiteSpace(payload.ImageUrl) ? "Text" : "Image",
                LikesCount = 0,
                CommentsCount = 0,
                SharesCount = 0,
                CreatedAt = now,
                UpdatedAt = null,
                IsDeleted = false,
                DeletedAt = null
            };

            await _db.Posts.AddAsync(post);
            await _db.SaveChangesAsync();

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(new ApiResponse<PostResponse>(
                new PostResponse(post.PostId, post.UserId, post.Content, post.ImageUrl, post.CreatedAt),
                null));
            return response;
        }

        [Function("ListPosts")]
        public async Task<HttpResponseData> ListPosts(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "posts")] HttpRequestData req)
        {
            {
                var unauthorized = await AuthGuard.Authorize(req, _config);
                if (unauthorized != null) return unauthorized;
            }

            var skip = GetIntQuery(req, "skip") ?? 0;
            var take = GetIntQuery(req, "take") ?? 50;
            take = Math.Clamp(take, 1, 200);
            skip = Math.Max(0, skip);

            var posts = await _db.Posts
                .OrderByDescending(p => p.CreatedAt)
                .Skip(skip)
                .Take(take)
                .Select(p => new PostResponse(p.PostId, p.UserId, p.Content, p.ImageUrl, p.CreatedAt))
                .ToListAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new ApiResponse<IEnumerable<PostResponse>>(posts, null));
            return response;
        }

        [Function("UpdatePost")]
        public async Task<HttpResponseData> UpdatePost(
            [HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "posts/{id:guid}")] HttpRequestData req,
            Guid id)
        {
            {
                var unauthorized = await AuthGuard.Authorize(req, _config);
                if (unauthorized != null) return unauthorized;
            }

            var payload = await req.ReadFromJsonAsync<CreatePostRequest>();
            if (payload == null)
            {
                return await Error(req, HttpStatusCode.BadRequest, "Invalid JSON body.");
            }

            var post = await _db.Posts.FirstOrDefaultAsync(p => p.PostId == id);
            if (post == null)
            {
                return await Error(req, HttpStatusCode.NotFound, "post not found");
            }

            if (!string.IsNullOrWhiteSpace(payload.Caption))
            {
                post.Content = payload.Caption.Trim();
            }
            if (payload.ImageUrl != null)
            {
                post.ImageUrl = string.IsNullOrWhiteSpace(payload.ImageUrl) ? null : payload.ImageUrl.Trim();
                post.PostType = string.IsNullOrWhiteSpace(post.ImageUrl) ? "Text" : "Image";
            }
            post.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new ApiResponse<PostResponse>(
                new PostResponse(post.PostId, post.UserId, post.Content, post.ImageUrl, post.CreatedAt),
                null));
            return response;
        }

        [Function("DeletePost")]
        public async Task<HttpResponseData> DeletePost(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "posts/{id:guid}")] HttpRequestData req,
            Guid id)
        {
            {
                var unauthorized = await AuthGuard.Authorize(req, _config);
                if (unauthorized != null) return unauthorized;
            }

            var post = await _db.Posts.FirstOrDefaultAsync(p => p.PostId == id);
            if (post == null)
            {
                return await Error(req, HttpStatusCode.NotFound, "post not found");
            }

            _db.Posts.Remove(post);
            await _db.SaveChangesAsync();

            var response = req.CreateResponse(HttpStatusCode.NoContent);
            return response;
        }

        private static int? GetIntQuery(HttpRequestData req, string key)
        {
            if (req.Url.Query is { Length: > 1 })
            {
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                if (int.TryParse(query[key], out var value))
                {
                    return value;
                }
            }
            return null;
        }

        private static async Task<HttpResponseData> Error(HttpRequestData req, HttpStatusCode status, string message, object? details = null)
        {
            var res = req.CreateResponse(status);
            await res.WriteAsJsonAsync(new ApiResponse<object>(null, new ApiError(message, details)));
            return res;
        }
    }
}
