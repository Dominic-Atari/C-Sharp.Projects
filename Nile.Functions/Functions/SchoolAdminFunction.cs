using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Nile.Database.DataContracts;
using Nile.Database.Entities;
using Nile.Functions.Functions.Infrastructure;
using Nile.Functions.Functions.Models;

namespace Nile.Functions.Functions;

public class SchoolAdminFunction
{
    private readonly DatabaseContext _db;
    private readonly IConfiguration _config;

    private static readonly Guid StudentRoleId = Guid.Parse("b8d9f9e3-7a3e-4c1b-9d7a-2f8d2f0c9e13");
    private static readonly Guid TeacherRoleId = Guid.Parse("2c0a9db6-6f45-4d29-9e11-3e6f0b1e5f22");

    public SchoolAdminFunction(DatabaseContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [Function("CreateStudent")]
    public async Task<HttpResponseData> CreateStudent(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "schools/{schoolId:guid}/students")] HttpRequestData req,
        Guid schoolId)
    {
        var principalResult = await AuthorizeHeadTeacher(req, schoolId);
        if (principalResult.Unauthorized != null) return principalResult.Unauthorized;

        var payload = await req.ReadFromJsonAsync<CreatePersonRequest>();
        if (payload == null)
        {
            return await Error(req, HttpStatusCode.BadRequest, "Invalid JSON body.");
        }

        var validationError = ValidatePersonPayload(payload);
        if (validationError != null) return await Error(req, HttpStatusCode.BadRequest, validationError);

        var exists = await _db.Users.AnyAsync(u => u.Username == payload.Username);
        if (exists) return await Error(req, HttpStatusCode.Conflict, "username already exists");

        var now = DateTimeOffset.UtcNow;
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Username = payload.Username.Trim(), CreatedAt = now };
        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FirstName = payload.FirstName.Trim(),
            LastName = payload.LastName.Trim(),
        };
        var pwd = new Passwords { UserId = userId, PasswordHash = PasswordHasher.Hash(payload.Password) };
        var membership = new SchoolMembership
        {
            SchoolMembershipId = Guid.NewGuid(),
            SchoolId = schoolId,
            UserId = userId,
            RoleInSchool = SchoolRole.Student,
            CreatedAt = DateTime.UtcNow,
        };
        var userRole = new UserRole { UserId = userId, RoleId = StudentRoleId };

        await _db.Users.AddAsync(user);
        await _db.UserProfiles.AddAsync(profile);
        await _db.Passwords.AddAsync(pwd);
        await _db.SchoolMemberships.AddAsync(membership);
        await _db.UserRoles.AddAsync(userRole);
        await _db.SaveChangesAsync();

        var res = req.CreateResponse(HttpStatusCode.Created);
        await res.WriteAsJsonAsync(new ApiResponse<object>(new { userId, username = user.Username }, null));
        return res;
    }

    [Function("CreateTeacher")]
    public async Task<HttpResponseData> CreateTeacher(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "schools/{schoolId:guid}/teachers")] HttpRequestData req,
        Guid schoolId)
    {
        var principalResult = await AuthorizeHeadTeacher(req, schoolId);
        if (principalResult.Unauthorized != null) return principalResult.Unauthorized;

        var payload = await req.ReadFromJsonAsync<CreatePersonRequest>();
        if (payload == null) return await Error(req, HttpStatusCode.BadRequest, "Invalid JSON body.");

        var validationError = ValidatePersonPayload(payload);
        if (validationError != null) return await Error(req, HttpStatusCode.BadRequest, validationError);

        var exists = await _db.Users.AnyAsync(u => u.Username == payload.Username);
        if (exists) return await Error(req, HttpStatusCode.Conflict, "username already exists");

        var now = DateTimeOffset.UtcNow;
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Username = payload.Username.Trim(), CreatedAt = now };
        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FirstName = payload.FirstName.Trim(),
            LastName = payload.LastName.Trim(),
        };
        var pwd = new Passwords { UserId = userId, PasswordHash = PasswordHasher.Hash(payload.Password) };
        var membership = new SchoolMembership
        {
            SchoolMembershipId = Guid.NewGuid(),
            SchoolId = schoolId,
            UserId = userId,
            RoleInSchool = SchoolRole.Teacher,
            CreatedAt = DateTime.UtcNow,
        };
        var userRole = new UserRole { UserId = userId, RoleId = TeacherRoleId };

        await _db.Users.AddAsync(user);
        await _db.UserProfiles.AddAsync(profile);
        await _db.Passwords.AddAsync(pwd);
        await _db.SchoolMemberships.AddAsync(membership);
        await _db.UserRoles.AddAsync(userRole);
        await _db.SaveChangesAsync();

        var res = req.CreateResponse(HttpStatusCode.Created);
        await res.WriteAsJsonAsync(new ApiResponse<object>(new { userId, username = user.Username }, null));
        return res;
    }

    [Function("GetTeachers")]
    public async Task<HttpResponseData> GetTeachers(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "schools/{schoolId:guid}/teachers")] HttpRequestData req,
        Guid schoolId)
    {
        var principalResult = await AuthorizeHeadTeacher(req, schoolId);
        if (principalResult.Unauthorized != null) return principalResult.Unauthorized;

        // Return list of teachers for the school (userId and username, and optional name)
        var q = from sm in _db.SchoolMemberships
                where sm.SchoolId == schoolId && sm.RoleInSchool == SchoolRole.Teacher
                join u in _db.Users on sm.UserId equals u.Id
                join p in _db.UserProfiles on u.Id equals p.UserId into up
                from prof in up.DefaultIfEmpty()
                select new { userId = u.Id, username = u.Username, firstName = prof != null ? prof.FirstName : null, lastName = prof != null ? prof.LastName : null };

        var list = await q.ToListAsync();

        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(new ApiResponse<object>(list, null));
        return res;
    }

    [Function("GetSubjects")]
    public async Task<HttpResponseData> GetSubjects(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "schools/{schoolId:guid}/subjects")] HttpRequestData req,
        Guid schoolId)
    {
        var principalResult = await AuthorizeHeadTeacher(req, schoolId);
        if (principalResult.Unauthorized != null) return principalResult.Unauthorized;

        var q = _db.Subjects.Select(s => new { subjectId = s.SubjectId, name = s.Name, stage = (int)s.Stage, description = s.Description });
        var list = await q.ToListAsync();

        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(new ApiResponse<object>(list, null));
        return res;
    }

    [Function("CreateSubject")]
    public async Task<HttpResponseData> CreateSubject(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "schools/{schoolId:guid}/subjects")] HttpRequestData req,
        Guid schoolId)
    {
        var principalResult = await AuthorizeHeadTeacher(req, schoolId);
        if (principalResult.Unauthorized != null) return principalResult.Unauthorized;

        var payload = await req.ReadFromJsonAsync<CreateSubjectRequest>();
        if (payload == null) return await Error(req, HttpStatusCode.BadRequest, "Invalid JSON body.");
        if (string.IsNullOrWhiteSpace(payload.Name)) return await Error(req, HttpStatusCode.BadRequest, "name is required.");

        if (!Enum.IsDefined(typeof(EducationStage), payload.Stage))
        {
            return await Error(req, HttpStatusCode.BadRequest, "stage is invalid.");
        }

        var existing = await _db.Subjects.FirstOrDefaultAsync(s => s.Name == payload.Name.Trim() && (int)s.Stage == payload.Stage);
        if (existing != null)
        {
            var resExists = req.CreateResponse(HttpStatusCode.OK);
            await resExists.WriteAsJsonAsync(new ApiResponse<object>(new { subjectId = existing.SubjectId }, null));
            return resExists;
        }

        var subject = new Subject
        {
            SubjectId = Guid.NewGuid(),
            Name = payload.Name.Trim(),
            Stage = (EducationStage)payload.Stage,
            Description = payload.Description?.Trim(),
        };
        await _db.Subjects.AddAsync(subject);
        await _db.SaveChangesAsync();

        var res = req.CreateResponse(HttpStatusCode.Created);
        await res.WriteAsJsonAsync(new ApiResponse<object>(new { subjectId = subject.SubjectId }, null));
        return res;
    }

    [Function("AssignTeacherSubject")]
    public async Task<HttpResponseData> AssignTeacherSubject(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "schools/{schoolId:guid}/assign-teacher")] HttpRequestData req,
        Guid schoolId)
    {
        var principalResult = await AuthorizeHeadTeacher(req, schoolId);
        if (principalResult.Unauthorized != null) return principalResult.Unauthorized;

        // Read raw JSON and validate GUIDs explicitly so we can return a friendly 400 error
        AssignTeacherSubjectRequest? payload = null;
        try
        {
            var body = await req.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body)) return await Error(req, HttpStatusCode.BadRequest, "Invalid JSON body.");

            using var doc = System.Text.Json.JsonDocument.Parse(body);
            var root = doc.RootElement;

            Guid teacherGuid;
            Guid subjectGuid;

            if (!root.TryGetProperty("teacherId", out var tEl) || tEl.ValueKind != System.Text.Json.JsonValueKind.String ||
                !Guid.TryParse(tEl.GetString(), out teacherGuid))
            {
                return await Error(req, HttpStatusCode.BadRequest, "teacherId must be a valid GUID string.");
            }

            if (!root.TryGetProperty("subjectId", out var sEl) || sEl.ValueKind != System.Text.Json.JsonValueKind.String ||
                !Guid.TryParse(sEl.GetString(), out subjectGuid))
            {
                return await Error(req, HttpStatusCode.BadRequest, "subjectId must be a valid GUID string.");
            }

            payload = new AssignTeacherSubjectRequest(teacherGuid, subjectGuid);
        }
        catch (System.Text.Json.JsonException)
        {
            return await Error(req, HttpStatusCode.BadRequest, "Invalid JSON body.");
        }

        var teacher = await _db.Users.FirstOrDefaultAsync(u => u.Id == payload.TeacherId);
        if (teacher == null) return await Error(req, HttpStatusCode.NotFound, "teacher not found");

        var subject = await _db.Subjects.FirstOrDefaultAsync(s => s.SubjectId == payload.SubjectId);
        if (subject == null) return await Error(req, HttpStatusCode.NotFound, "subject not found");

        var membership = await _db.SchoolMemberships.FirstOrDefaultAsync(sm => sm.SchoolId == schoolId && sm.UserId == payload.TeacherId);
        if (membership == null)
        {
            // auto-enroll as teacher
            membership = new SchoolMembership
            {
                SchoolMembershipId = Guid.NewGuid(),
                SchoolId = schoolId,
                UserId = payload.TeacherId,
                RoleInSchool = SchoolRole.Teacher,
                CreatedAt = DateTime.UtcNow,
            };
            await _db.SchoolMemberships.AddAsync(membership);
        }

        var existingRole = await _db.UserRoles.AnyAsync(ur => ur.UserId == payload.TeacherId && ur.RoleId == TeacherRoleId);
        if (!existingRole)
        {
            await _db.UserRoles.AddAsync(new UserRole { UserId = payload.TeacherId, RoleId = TeacherRoleId });
        }

        var existingAssignment = await _db.TeacherSubjects.FirstOrDefaultAsync(ts => ts.UserId == payload.TeacherId && ts.SchoolId == schoolId && ts.SubjectId == payload.SubjectId);
        if (existingAssignment == null)
        {
            var assignment = new TeacherSubject
            {
                TeacherSubjectId = Guid.NewGuid(),
                UserId = payload.TeacherId,
                SchoolId = schoolId,
                SubjectId = payload.SubjectId,
                AssignedAt = DateTime.UtcNow,
            };
            await _db.TeacherSubjects.AddAsync(assignment);
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException dbEx)
        {
            // Return helpful error to client for debugging (message includes DB constraint info)
            return await Error(req, HttpStatusCode.InternalServerError, "Assignment failed", dbEx.Message);
        }
        catch (Exception ex)
        {
            return await Error(req, HttpStatusCode.InternalServerError, "Assignment failed", ex.Message);
        }

        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(new ApiResponse<object>(new { teacherId = payload.TeacherId, subjectId = payload.SubjectId }, null));
        return res;
    }

    [Function("CreateCourse")]
    public async Task<HttpResponseData> CreateCourse(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "schools/{schoolId:guid}/courses")] HttpRequestData req,
        Guid schoolId)
    {
        var principalResult = await AuthorizeHeadTeacher(req, schoolId);
        if (principalResult.Unauthorized != null) return principalResult.Unauthorized;

        var payload = await req.ReadFromJsonAsync<CreateCourseRequest>();
        if (payload == null) return await Error(req, HttpStatusCode.BadRequest, "Invalid JSON body.");
        if (string.IsNullOrWhiteSpace(payload.Title)) return await Error(req, HttpStatusCode.BadRequest, "title is required.");

        if (payload.SubjectId.HasValue)
        {
            var existsSubject = await _db.Subjects.AnyAsync(s => s.SubjectId == payload.SubjectId.Value);
            if (!existsSubject) return await Error(req, HttpStatusCode.BadRequest, "subjectId is invalid.");
        }

        var course = new Course
        {
            CourseId = Guid.NewGuid(),
            SchoolId = schoolId,
            SubjectId = payload.SubjectId,
            Title = payload.Title.Trim(),
            Summary = payload.Summary?.Trim(),
            Level = payload.Level?.Trim(),
            IsPublished = false,
            CreatedAt = DateTime.UtcNow,
        };
        await _db.Courses.AddAsync(course);
        await _db.SaveChangesAsync();

        var res = req.CreateResponse(HttpStatusCode.Created);
        await res.WriteAsJsonAsync(new ApiResponse<object>(new { courseId = course.CourseId }, null));
        return res;
    }

    [Function("CreateLesson")]
    public async Task<HttpResponseData> CreateLesson(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "courses/{courseId:guid}/lessons")] HttpRequestData req,
        Guid courseId)
    {
        // Need to determine school for authorization
        var course = await _db.Courses.FirstOrDefaultAsync(c => c.CourseId == courseId);
        if (course == null) return await Error(req, HttpStatusCode.NotFound, "course not found");

        var principalResult = await AuthorizeHeadTeacher(req, course.SchoolId);
        if (principalResult.Unauthorized != null) return principalResult.Unauthorized;

        var payload = await req.ReadFromJsonAsync<CreateLessonRequest>();
        if (payload == null) return await Error(req, HttpStatusCode.BadRequest, "Invalid JSON body.");
        if (string.IsNullOrWhiteSpace(payload.Title)) return await Error(req, HttpStatusCode.BadRequest, "title is required.");

        var lesson = new Lesson
        {
            LessonId = Guid.NewGuid(),
            CourseId = courseId,
            Title = payload.Title.Trim(),
            BodyMarkdown = payload.BodyMarkdown,
            ResourceUrl = payload.ResourceUrl,
            Order = payload.Order,
            DurationMinutes = payload.DurationMinutes,
            CreatedAt = DateTime.UtcNow,
        };

        await _db.Lessons.AddAsync(lesson);
        await _db.SaveChangesAsync();

        var res = req.CreateResponse(HttpStatusCode.Created);
        await res.WriteAsJsonAsync(new ApiResponse<object>(new { lessonId = lesson.LessonId }, null));
        return res;
    }

    private static string? ValidatePersonPayload(CreatePersonRequest payload)
    {
        if (string.IsNullOrWhiteSpace(payload.Username) || string.IsNullOrWhiteSpace(payload.Password) ||
            string.IsNullOrWhiteSpace(payload.FirstName) || string.IsNullOrWhiteSpace(payload.LastName))
        {
            return "username, password, firstName, and lastName are required.";
        }
        if (payload.Password.Length < 8)
        {
            return "password must be at least 8 characters.";
        }
        return null;
    }

    private async Task<(ClaimsPrincipal? Principal, HttpResponseData? Unauthorized)> AuthorizeHeadTeacher(HttpRequestData req, Guid schoolId)
    {
        var principal = await ValidateJwt(req);
        if (principal == null)
        {
            var unauthorized = await BuildUnauthorized(req);
            return (null, unauthorized);
        }

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier) ?? principal.FindFirst(JwtRegisteredClaimNames.Sub);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            var unauthorized = await BuildUnauthorized(req);
            return (null, unauthorized);
        }

        var isHeadTeacher = principal.Claims.Any(c => c.Type == ClaimTypes.Role && string.Equals(c.Value, "HeadTeacher", StringComparison.OrdinalIgnoreCase));
        if (!isHeadTeacher)
        {
            var unauthorized = await BuildUnauthorized(req);
            return (null, unauthorized);
        }

        var ownsSchool = await _db.Schools.AnyAsync(s => s.SchoolId == schoolId && s.HeadTeacherUserId == userId);
        if (!ownsSchool)
        {
            var unauthorized = await BuildUnauthorized(req);
            return (null, unauthorized);
        }

        return (principal, null);
    }

    private async Task<ClaimsPrincipal?> ValidateJwt(HttpRequestData req)
    {
        if (!req.Headers.TryGetValues("Authorization", out var values)) return null;
        var header = values.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return null;
        var token = header.Substring("Bearer ".Length).Trim();
        if (string.IsNullOrWhiteSpace(token)) return null;

        var secret = _config["Jwt:Secret"];
        if (string.IsNullOrWhiteSpace(secret)) return null;

        var issuer = _config["Jwt:Issuer"];
        var audience = _config["Jwt:Audience"];
        var parameters = new TokenValidationParameters
        {
            ValidIssuer = string.IsNullOrWhiteSpace(issuer) ? null : issuer,
            ValidateIssuer = !string.IsNullOrWhiteSpace(issuer),
            ValidAudience = string.IsNullOrWhiteSpace(audience) ? null : audience,
            ValidateAudience = !string.IsNullOrWhiteSpace(audience),
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, parameters, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    private static async Task<HttpResponseData> BuildUnauthorized(HttpRequestData req)
    {
        var res = req.CreateResponse(HttpStatusCode.Unauthorized);
        res.Headers.Add("WWW-Authenticate", "Bearer");
        await res.WriteStringAsync("Unauthorized");
        return res;
    }

    private static async Task<HttpResponseData> Error(HttpRequestData req, HttpStatusCode status, string message, object? details = null)
    {
        var res = req.CreateResponse(status);
        await res.WriteAsJsonAsync(new ApiResponse<object>(null, new ApiError(message, details)));
        return res;
    }
}
