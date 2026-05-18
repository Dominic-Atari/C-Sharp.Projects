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
    public class UsersFunction
    {
        private readonly DatabaseContext _db;
        private readonly IConfiguration _config;
        private const int MaxUsernameLength = 15;

        public UsersFunction(DatabaseContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        [Function("CreateUser")]
        public async Task<HttpResponseData> CreateUser(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "users")] HttpRequestData req)
        {
            var unauthorized = await AuthGuard.Authorize(req, _config);
            if (unauthorized != null) return unauthorized;

            var payload = await req.ReadFromJsonAsync<CreateUserRequest>();
            if (payload == null)
            {
                return await Error(req, HttpStatusCode.BadRequest, "Invalid JSON body.");
            }

            var username = payload.Username?.Trim();
            // Ensure username fits DB schema (NVARCHAR(15)) and normalize for consistent lookups
            const int MaxUsernameLength = 15;
            if (!string.IsNullOrWhiteSpace(username) && username.Length > MaxUsernameLength)
            {
                username = username.Substring(0, MaxUsernameLength);
            }
            if (!string.IsNullOrWhiteSpace(username)) username = username.ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(payload.FirstName) || string.IsNullOrWhiteSpace(payload.LastName))
            {
                return await Error(req, HttpStatusCode.BadRequest, "username, firstName, and lastName are required.");
            }

            // Ensure username uniqueness (compare using normalized username)
            var exists = await _db.Users.AnyAsync(u => u.Username == username);
            if (exists)
            {
                return await Error(req, HttpStatusCode.Conflict, "username already exists");
            }

            var now = DateTimeOffset.UtcNow;
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = username,
                CreatedAt = now
            };

            var profile = new UserProfile
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                FirstName = payload.FirstName.Trim(),
                LastName = payload.LastName.Trim(),
                Location = string.IsNullOrWhiteSpace(payload.Location) ? null : payload.Location.Trim(),
                Bio = null
            };

            await _db.Users.AddAsync(user);
            await _db.UserProfiles.AddAsync(profile);
            await _db.SaveChangesAsync();

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(new ApiResponse<UserResponse>(
                new UserResponse(user.Id, user.Username, profile.FirstName, profile.LastName, user.CreatedAt),
                null));
            return response;
        }

        [Function("ListUsers")]
        public async Task<HttpResponseData> ListUsers(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users")] HttpRequestData req)
        {
            var unauthorized = await AuthGuard.Authorize(req, _config);
            if (unauthorized != null) return unauthorized;

            var query = _db.Users
                .Include(u => u.Profile)
                .OrderBy(u => u.CreatedAt);

            var skip = GetIntQuery(req, "skip") ?? 0;
            var take = GetIntQuery(req, "take") ?? 50;
            take = Math.Clamp(take, 1, 200);
            skip = Math.Max(0, skip);

            var users = await query
                .Skip(skip)
                .Take(take)
                .Select(u => new UserResponse(
                    u.Id,
                    u.Username,
                    u.Profile != null ? u.Profile.FirstName : string.Empty,
                    u.Profile != null ? u.Profile.LastName : string.Empty,
                    u.CreatedAt))
                .ToListAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new ApiResponse<IEnumerable<UserResponse>>(users, null));
            return response;
        }

        [Function("UpdateUser")]
        public async Task<HttpResponseData> UpdateUser(
            [HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = "users/{id:guid}")] HttpRequestData req,
            Guid id)
        {
            var unauthorized = await AuthGuard.Authorize(req, _config);
            if (unauthorized != null) return unauthorized;

            var payload = await req.ReadFromJsonAsync<CreateUserRequest>();
            if (payload == null)
            {
                return await Error(req, HttpStatusCode.BadRequest, "Invalid JSON body.");
            }

            var user = await _db.Users.Include(u => u.Profile).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return await Error(req, HttpStatusCode.NotFound, "user not found");
            }

            if (!string.IsNullOrWhiteSpace(payload.Username))
            {
                var newUsername = payload.Username.Trim();
                if (newUsername.Length > MaxUsernameLength) newUsername = newUsername.Substring(0, MaxUsernameLength);
                newUsername = newUsername.ToLowerInvariant();

                var exists = await _db.Users.AnyAsync(u => u.Username == newUsername && u.Id != id);
                if (exists)
                {
                    return await Error(req, HttpStatusCode.Conflict, "username already exists");
                }
                user.Username = newUsername;
            }

            user.Profile ??= new UserProfile { Id = Guid.NewGuid(), UserId = user.Id };
            if (!string.IsNullOrWhiteSpace(payload.FirstName)) user.Profile.FirstName = payload.FirstName.Trim();
            if (!string.IsNullOrWhiteSpace(payload.LastName)) user.Profile.LastName = payload.LastName.Trim();
            user.Profile.Location = string.IsNullOrWhiteSpace(payload.Location) ? user.Profile.Location : payload.Location.Trim();

            await _db.SaveChangesAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new ApiResponse<UserResponse>(
                new UserResponse(user.Id, user.Username, user.Profile.FirstName, user.Profile.LastName, user.CreatedAt),
                null));
            return response;
        }

        [Function("DeleteUser")]
        public async Task<HttpResponseData> DeleteUser(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "users/{id:guid}")] HttpRequestData req,
            Guid id)
        {
            var unauthorized = await AuthGuard.Authorize(req, _config);
            if (unauthorized != null) return unauthorized;

            var user = await _db.Users.Include(u => u.Profile).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return await Error(req, HttpStatusCode.NotFound, "user not found");
            }

            _db.Users.Remove(user);
            if (user.Profile != null) _db.UserProfiles.Remove(user.Profile);
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
