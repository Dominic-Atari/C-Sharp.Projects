using System.Net;
using System.Net.Http.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nile.Database.DataContracts;
using Nile.Database.Entities;
using Nile.Functions.Functions.Infrastructure;
using Nile.Functions.Functions.Models;

namespace Nile.Functions.Functions;

public class AuthFunction
{
    private readonly DatabaseContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthFunction> _logger;

    private static readonly Guid HeadTeacherRoleId = Guid.Parse("4f6e2ad7-8bc0-4b3f-9d4c-4d6c1c6b0f01");

    public AuthFunction(DatabaseContext db, IConfiguration config, ILogger<AuthFunction> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    [Function("RegisterHead")]
    public async Task<HttpResponseData> RegisterHead(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/register-head")] HttpRequestData req)
    {
        // Require API key if configured
        var unauthorized = await AuthGuard.IsAuthorized(req, _config);
        if (unauthorized != null) return unauthorized;

        var payload = await req.ReadFromJsonAsync<RegisterHeadRequest>();
        if (payload == null)
        {
            return await Error(req, HttpStatusCode.BadRequest, "Invalid JSON body.");
        }

        var username = payload.Username?.Trim();
        // Ensure username fits DB schema (NVARCHAR(15)) to avoid truncation errors
        const int MaxUsernameLength = 15;
        if (!string.IsNullOrWhiteSpace(username) && username.Length > MaxUsernameLength)
        {
            _logger?.LogInformation("RegisterHead: trimming username from {Orig} to {Trimmed}", username, username.Substring(0, MaxUsernameLength));
            username = username.Substring(0, MaxUsernameLength);
        }
        // Normalize username for consistent lookups (trim already applied)
        if (!string.IsNullOrWhiteSpace(username))
        {
            username = username.ToLowerInvariant();
        }
        var password = payload.Password;
        var first = payload.FirstName?.Trim();
        var last = payload.LastName?.Trim();
        var schoolName = payload.SchoolName?.Trim();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(last) || string.IsNullOrWhiteSpace(schoolName))
        {
            return await Error(req, HttpStatusCode.BadRequest, "username, password, firstName, lastName, and schoolName are required.");
        }

        if (!IsPasswordStrong(password))
        {
            return await Error(req, HttpStatusCode.BadRequest, "password must be at least 8 characters.");
        }

        var exists = await _db.Users.AnyAsync(u => u.Username == username);
        if (exists)
        {
            _logger?.LogInformation("RegisterHead: attempted to register existing username={Username}", username);
            return await Error(req, HttpStatusCode.Conflict, "username already exists");
        }

        var now = DateTimeOffset.UtcNow;
        var userId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Username = username,
            CreatedAt = now,
        };

        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FirstName = first!,
            LastName = last!,
        };

        var passwordHash = PasswordHasher.Hash(password);
        var passwordRow = new Passwords
        {
            UserId = userId,
            PasswordHash = passwordHash
        };

        var schoolId = Guid.NewGuid();
        var school = new School
        {
            SchoolId = schoolId,
            SchoolName = schoolName!,
            SchoolAddress = string.Empty,
            City = string.Empty,
            State = string.Empty,
            Country = string.Empty,
            County = string.Empty,
            ZipCode = string.Empty,
            PhoneNumber = string.Empty,
            Email = string.Empty,
            Description = string.Empty,
            ImageUrl = string.Empty,
            HeadTeacherUserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false,
            DeletedAt = null,
        };

        var membership = new SchoolMembership
        {
            SchoolMembershipId = Guid.NewGuid(),
            SchoolId = schoolId,
            UserId = userId,
            RoleInSchool = SchoolRole.HeadTeacher,
            CreatedAt = DateTime.UtcNow,
        };

        var userRole = new UserRole
        {
            UserId = userId,
            RoleId = HeadTeacherRoleId,
        };

        await _db.Users.AddAsync(user);
        await _db.UserProfiles.AddAsync(profile);
        await _db.Passwords.AddAsync(passwordRow);
        await _db.Schools.AddAsync(school);
        await _db.SchoolMemberships.AddAsync(membership);
        await _db.UserRoles.AddAsync(userRole);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException dbEx)
        {
            _logger?.LogError(dbEx, "RegisterHead: DbUpdateException while saving user {Username}", username);
            return await Error(req, HttpStatusCode.Conflict, "Registration failed", dbEx.Message);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "RegisterHead: Unexpected error while saving user {Username}", username);
            return await Error(req, HttpStatusCode.InternalServerError, "Registration failed", ex.Message);
        }

        _logger?.LogInformation("RegisterHead: created user {Username} Id={UserId}", username, userId);

        var roles = new[] { "HeadTeacher" };
        var token = JwtTokenService.IssueToken(user, roles, schoolId, _config);

        // Suggest client redirect destination for HeadTeacher users
        string? redirectUrl = null;
        if (roles.Any(r => string.Equals(r, "HeadTeacher", StringComparison.OrdinalIgnoreCase)))
        {
            // SPA route on the client; frontend should navigate to this path after login
            redirectUrl = "/admin";
        }

        var res = req.CreateResponse(HttpStatusCode.Created);
        await res.WriteAsJsonAsync(new ApiResponse<AuthResponse>(new AuthResponse(token, userId, schoolId, username, roles, redirectUrl), null));
        return res;
    }

    [Function("Login")]
    public async Task<HttpResponseData> Login(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/login")] HttpRequestData req)
    {
        var payload = await req.ReadFromJsonAsync<LoginRequest>();
        if (payload == null)
        {
            return await Error(req, HttpStatusCode.BadRequest, "Invalid JSON body.");
        }

        var username = payload.Username?.Trim();
        if (!string.IsNullOrWhiteSpace(username))
        {
            username = username.ToLowerInvariant();
        }
        var password = payload.Password;
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return await Error(req, HttpStatusCode.BadRequest, "username and password are required.");
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
        {
            return await Error(req, HttpStatusCode.Unauthorized, "Invalid credentials.");
        }

        var pwd = await _db.Passwords.FirstOrDefaultAsync(p => p.UserId == user.Id);
        if (pwd == null || !PasswordHasher.Verify(password, pwd.PasswordHash))
        {
            return await Error(req, HttpStatusCode.Unauthorized, "Invalid credentials.");
        }

        var roles = await _db.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Join(_db.Roles, ur => ur.RoleId, r => r.RoleId, (ur, r) => r.RoleName)
            .ToListAsync();

        var schoolId = await _db.SchoolMemberships
            .Where(sm => sm.UserId == user.Id)
            .Select(sm => (Guid?)sm.SchoolId)
            .FirstOrDefaultAsync();

        var token = JwtTokenService.IssueToken(user, roles, schoolId, _config);

        string? redirectUrl = null;
        if (roles.Any(r => string.Equals(r, "HeadTeacher", StringComparison.OrdinalIgnoreCase)))
        {
            redirectUrl = "/admin";
        }

        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(new ApiResponse<AuthResponse>(
            new AuthResponse(token, user.Id, schoolId, user.Username, roles, redirectUrl),
            null));
        return res;
    }

    private static bool IsPasswordStrong(string password) => !string.IsNullOrEmpty(password) && password.Length >= 8;

    private async Task<HttpResponseData> Error(HttpRequestData req, HttpStatusCode status, string message, object? details = null)
    {
        var res = req.CreateResponse(status);
        AddCorsFallback(res, req);
        await res.WriteAsJsonAsync(new ApiResponse<object>(null, new ApiError(message, details)));
        return res;
    }

    // Temporary per-endpoint fallback: echo Origin into response when allowed.
    // This helps browsers read responses while middleware/host issues are resolved.
    private void AddCorsFallback(HttpResponseData res, HttpRequestData req)
    {
        try
        {
            if (!req.Headers.TryGetValues("Origin", out var origins)) return;
            var origin = origins.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(origin)) return;

            var configured = _config["Cors:AllowedOrigins"] ?? _config["CORS"] ?? _config["Host:CORS"];
            if (string.IsNullOrWhiteSpace(configured)) return;

            var allowed = configured.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (!allowed.Any(a => string.Equals(a, "*", StringComparison.Ordinal) || string.Equals(a, origin, StringComparison.OrdinalIgnoreCase))) return;

            if (!res.Headers.TryGetValues("Access-Control-Allow-Origin", out _))
                res.Headers.Add("Access-Control-Allow-Origin", origin);
            if (!res.Headers.TryGetValues("Vary", out _))
                res.Headers.Add("Vary", "Origin");
            if (!res.Headers.TryGetValues("Access-Control-Allow-Headers", out _))
                res.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization, x-api-key");
            if (!res.Headers.TryGetValues("Access-Control-Allow-Methods", out _))
                res.Headers.Add("Access-Control-Allow-Methods", "GET,POST,PUT,PATCH,DELETE,OPTIONS");
        }
        catch
        {
            // Best-effort only; don't throw from error handling
        }
    }
}
