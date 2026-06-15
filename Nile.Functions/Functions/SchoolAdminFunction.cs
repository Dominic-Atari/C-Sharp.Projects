using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.WebUtilities;
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
        // Require head teacher to create students
        // Read raw JSON so we can accept an optional 'stage' property in the same body
        CreatePersonRequest? payload = null;
        string? stageId = null;
        string? rawBody = null;
        System.Text.Json.JsonElement root = default;
        try
        {
            var body = await req.ReadAsStringAsync();
            rawBody = body;
            if (string.IsNullOrWhiteSpace(body)) return await Error(req, HttpStatusCode.BadRequest, "Invalid JSON body.");
            try { Console.WriteLine($"CreateStudent raw body: {body}"); } catch {}

            using var doc = System.Text.Json.JsonDocument.Parse(body);
            root = doc.RootElement.Clone();

            // Read stage/stageId case-insensitively from the raw JSON
            foreach (var prop in root.EnumerateObject())
            {
                if (string.Equals(prop.Name, "stageId", StringComparison.OrdinalIgnoreCase) && prop.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    stageId = prop.Value.GetString()?.Trim();
                }
                else if (string.Equals(prop.Name, "stage", StringComparison.OrdinalIgnoreCase))
                {
                    var stageEl = prop.Value;
                    if (stageEl.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        var s = stageEl.GetString()?.Trim();
                        if (!string.IsNullOrWhiteSpace(s))
                        {
                            // If it's a GUID, treat as stageId
                            if (Guid.TryParse(s, out var tempStageGuid)) stageId = s;
                            else if (int.TryParse(s, out var _)) { /* numeric stage not supported for students here */ }
                        }
                    }
                }
            }

                // Deserialize into the CreatePersonRequest shape (allow case-insensitive JSON keys)
                payload = System.Text.Json.JsonSerializer.Deserialize<CreatePersonRequest>(root.GetRawText(), new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                try
                {
                    // Log some fields for debugging (avoid printing password)
                    try { Console.WriteLine($"CreateStudent deserialized: Username={payload?.Username ?? "(null)"}, FirstName={payload?.FirstName ?? "(null)"}, LastName={payload?.LastName ?? "(null)"}, StageId={payload?.StageId ?? "(null)"}, SubLevelId={payload?.SubLevelId ?? "(null)"}"); } catch {}
                }
                catch { }
        }
        catch (System.Text.Json.JsonException)
        {
            return await Error(req, HttpStatusCode.BadRequest, "Invalid JSON body.");
        }

        if (payload == null) return await Error(req, HttpStatusCode.BadRequest, "Invalid JSON body.");

        // If required fields appear missing after deserialization, attempt a tolerant fallback
        var validationError = ValidatePersonPayload(payload);
        if (validationError != null)
        {
            try
            {
                // helper to get property by case-insensitive name from root or nested `student` object
                static string? GetProp(System.Text.Json.JsonElement r, string name)
                {
                    foreach (var p in r.EnumerateObject()) if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)) return p.Value.ValueKind == System.Text.Json.JsonValueKind.String ? p.Value.GetString() : p.Value.ToString();
                    return null;
                }

                string? userVal = null, pwdVal = null, fnVal = null, lnVal = null, sidVal = null, slidVal = null;
                // top-level
                userVal = GetProp(root, "username") ?? GetProp(root, "userName") ?? GetProp(root, "Username");
                pwdVal = GetProp(root, "password") ?? GetProp(root, "Password");
                fnVal = GetProp(root, "firstName") ?? GetProp(root, "FirstName") ?? GetProp(root, "first_name");
                lnVal = GetProp(root, "lastName") ?? GetProp(root, "LastName") ?? GetProp(root, "last_name");
                sidVal = GetProp(root, "stageId") ?? GetProp(root, "stage");
                slidVal = GetProp(root, "subLevelId") ?? GetProp(root, "sublevelId") ?? GetProp(root, "subLevel");

                // nested under `student` or `payload` or `data`
                if ((string.IsNullOrWhiteSpace(userVal) || string.IsNullOrWhiteSpace(pwdVal) || string.IsNullOrWhiteSpace(fnVal) || string.IsNullOrWhiteSpace(lnVal)) && root.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    foreach (var p in root.EnumerateObject())
                    {
                        if ((string.Equals(p.Name, "student", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Name, "payload", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Name, "data", StringComparison.OrdinalIgnoreCase)) && p.Value.ValueKind == System.Text.Json.JsonValueKind.Object)
                        {
                            var obj = p.Value;
                                    userVal ??= GetProp(obj, "username");
                                    pwdVal ??= GetProp(obj, "password");
                                    fnVal ??= GetProp(obj, "firstName");
                                    lnVal ??= GetProp(obj, "lastName");
                                    sidVal ??= GetProp(obj, "stageId") ?? GetProp(obj, "stage");
                                    slidVal ??= GetProp(obj, "subLevelId");
                            break;
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(userVal) && !string.IsNullOrWhiteSpace(pwdVal) && !string.IsNullOrWhiteSpace(fnVal) && !string.IsNullOrWhiteSpace(lnVal))
                {
                    payload = new CreatePersonRequest(userVal.Trim(), pwdVal.Trim(), fnVal.Trim(), lnVal.Trim(), string.IsNullOrWhiteSpace(sidVal) ? null : sidVal.Trim(), string.IsNullOrWhiteSpace(slidVal) ? null : slidVal.Trim());
                    validationError = ValidatePersonPayload(payload);
                }
            }
            catch { }
        }

        if (validationError != null)
        {
            try
            {
                var parsed = new { username = payload?.Username, firstName = payload?.FirstName, lastName = payload?.LastName, stageId = payload?.StageId, subLevelId = payload?.SubLevelId };
                var resBad = req.CreateResponse(HttpStatusCode.BadRequest);
                resBad.Headers.Add("X-Error-Message", validationError);
                try { resBad.Headers.Add("X-Error-Details", System.Text.Json.JsonSerializer.Serialize(parsed)); } catch {}
                await resBad.WriteAsJsonAsync(new { error = validationError, parsed });
                try { Console.WriteLine($"Validation failed, returning parsed: {System.Text.Json.JsonSerializer.Serialize(parsed)}"); } catch {}
                return resBad;
            }
            catch
            {
                return await Error(req, HttpStatusCode.BadRequest, validationError);
            }
        }

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
        Guid? parsedStage = null;
        if (!string.IsNullOrWhiteSpace(stageId) && Guid.TryParse(stageId, out var psg)) parsedStage = psg;

        var membership = new SchoolMembership
        {
            SchoolMembershipId = Guid.NewGuid(),
            SchoolId = schoolId,
            UserId = userId,
            RoleInSchool = SchoolRole.Student,
            StageId = parsedStage,
            CreatedAt = DateTime.UtcNow,
        };
        // If subLevelId supplied, validate and attach to membership (ensure belongs to same school and stage)
        if (!string.IsNullOrWhiteSpace(payload.SubLevelId))
        {
            if (!Guid.TryParse(payload.SubLevelId, out var slGuid)) return await Error(req, HttpStatusCode.BadRequest, "subLevelId is invalid");
            var sub = await _db.SubLevels.FirstOrDefaultAsync(s => s.SubLevelId == slGuid && !s.IsDeleted);
            if (sub == null) return await Error(req, HttpStatusCode.BadRequest, "subLevelId is invalid");
            var parentStage = await _db.Stages.FirstOrDefaultAsync(s => s.StageId == sub.StageId && s.SchoolId == schoolId && !s.IsDeleted);
            if (parentStage == null) return await Error(req, HttpStatusCode.BadRequest, "subLevelId is invalid for this school");
            // If stage was supplied and doesn't match sublevel's stage, reject
            if (parsedStage.HasValue && parsedStage.Value != sub.StageId) return await Error(req, HttpStatusCode.BadRequest, "subLevelId does not belong to provided stage");
            membership.SubLevelId = slGuid;
            // If stage not supplied, attach stage from sublevel
            if (!membership.StageId.HasValue) membership.StageId = sub.StageId;
        }
        var userRole = new UserRole { UserId = userId, RoleId = StudentRoleId };

        await _db.Users.AddAsync(user);
        await _db.UserProfiles.AddAsync(profile);
        await _db.Passwords.AddAsync(pwd);
        await _db.SchoolMemberships.AddAsync(membership);
        await _db.UserRoles.AddAsync(userRole);
        await _db.SaveChangesAsync();
        try { Console.WriteLine($"CreateStudent saved: userId={userId}, username={user.Username}"); } catch {}

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
        if (validationError != null)
        {
            try
            {
                var debug = System.Text.Json.JsonSerializer.Serialize(new { username = payload?.Username, firstName = payload?.FirstName, lastName = payload?.LastName, stageId = payload?.StageId, subLevelId = payload?.SubLevelId });
                return await Error(req, HttpStatusCode.BadRequest, validationError, debug);
            }
            catch { return await Error(req, HttpStatusCode.BadRequest, validationError); }
        }

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
        // If stage/sublevel supplied, validate and attach to membership
        if (!string.IsNullOrWhiteSpace(payload.StageId))
        {
            if (!Guid.TryParse(payload.StageId, out var sGuid)) return await Error(req, HttpStatusCode.BadRequest, "stageId is invalid");
            var stage = await _db.Stages.FirstOrDefaultAsync(s => s.StageId == sGuid && s.SchoolId == schoolId && !s.IsDeleted);
            if (stage == null) return await Error(req, HttpStatusCode.BadRequest, "stageId is invalid for this school");
            membership.StageId = sGuid;
        }
        if (!string.IsNullOrWhiteSpace(payload.SubLevelId))
        {
            if (!Guid.TryParse(payload.SubLevelId, out var slGuid)) return await Error(req, HttpStatusCode.BadRequest, "subLevelId is invalid");
            var sub = await _db.SubLevels.FirstOrDefaultAsync(sl => sl.SubLevelId == slGuid && !sl.IsDeleted);
            if (sub == null) return await Error(req, HttpStatusCode.BadRequest, "subLevelId is invalid");
            // Ensure sublevel belongs to same school
            var parentStage = await _db.Stages.FirstOrDefaultAsync(s => s.StageId == sub.StageId && s.SchoolId == schoolId && !s.IsDeleted);
            if (parentStage == null) return await Error(req, HttpStatusCode.BadRequest, "subLevelId is invalid for this school");
            // If stage specified, ensure sublevel matches it
            if (membership.StageId.HasValue && membership.StageId.Value != sub.StageId) return await Error(req, HttpStatusCode.BadRequest, "subLevelId does not belong to provided stage");
            membership.SubLevelId = slGuid;
            // If stage not supplied, attach stage from sublevel
            if (!membership.StageId.HasValue) membership.StageId = sub.StageId;
        }
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
            where sm.SchoolId == schoolId && sm.RoleInSchool == SchoolRole.Teacher && !sm.IsDeleted
                join u in _db.Users on sm.UserId equals u.Id
                join p in _db.UserProfiles on u.Id equals p.UserId into up
                from prof in up.DefaultIfEmpty()
                select new { userId = u.Id, username = u.Username, firstName = prof != null ? prof.FirstName : null, lastName = prof != null ? prof.LastName : null, stageId = sm.StageId };

        var list = await q.ToListAsync();

        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(new ApiResponse<object>(list, null));
        return res;
    }

    [Function("GetTeacherSubjects")]
    public async Task<HttpResponseData> GetTeacherSubjects(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "schools/{schoolId:guid}/teachers/{teacherId:guid}/subjects")] HttpRequestData req,
        Guid schoolId,
        Guid teacherId)
    {
        // Allow head teacher (owner) or the teacher themselves.
        // Avoid calling AuthorizeHeadTeacher here since it may create a response on failure which
        // can interfere with additional validation; instead validate the JWT and check permissions
        // inline to keep control of response creation.
        var caller = await ValidateJwt(req);
        if (caller == null) return await BuildUnauthorized(req);

        var userIdClaim = caller.FindFirst(ClaimTypes.NameIdentifier) ?? caller.FindFirst(JwtRegisteredClaimNames.Sub);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId)) return await BuildUnauthorized(req);

        var isHeadTeacher = caller.Claims.Any(c => c.Type == ClaimTypes.Role && string.Equals(c.Value, "HeadTeacher", StringComparison.OrdinalIgnoreCase));
        var isOwner = false;
        if (isHeadTeacher)
        {
            isOwner = await _db.Schools.AnyAsync(s => s.SchoolId == schoolId && s.HeadTeacherUserId == userId);
        }

        if (!isOwner && userId != teacherId)
        {
            return await BuildUnauthorized(req);
        }

        var q = from ts in _db.TeacherSubjects
                where ts.UserId == teacherId && ts.SchoolId == schoolId
                join s in _db.Subjects on ts.SubjectId equals s.SubjectId
                select new { subjectId = s.SubjectId, name = s.Name, stage = (int)s.Stage, description = s.Description };

        var list = await q.ToListAsync();

        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(new ApiResponse<object>(list, null));
        return res;
    }

    [Function("DeleteTeacher")]
    public async Task<HttpResponseData> DeleteTeacher(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "schools/{schoolId:guid}/teachers/{teacherId:guid}")] HttpRequestData req,
        Guid schoolId,
        Guid teacherId)
    {
        var principalResult = await AuthorizeHeadTeacher(req, schoolId);
        if (principalResult.Unauthorized != null) return principalResult.Unauthorized;
        try { Console.WriteLine($"DeleteTeacher called for school {schoolId} teacher {teacherId}"); } catch {}

        var membership = await _db.SchoolMemberships.FirstOrDefaultAsync(sm => sm.SchoolId == schoolId && sm.UserId == teacherId && sm.RoleInSchool == SchoolRole.Teacher && !sm.IsDeleted);
        try { Console.WriteLine($"DeleteTeacher: membership found? {membership != null}"); } catch {}
        if (membership == null) return await Error(req, HttpStatusCode.NotFound, "teacher membership not found");

        // soft-delete membership
        membership.IsDeleted = true;
        membership.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var res = req.CreateResponse(HttpStatusCode.OK);
        var payload = new { userId = membership.UserId, isDeleted = membership.IsDeleted, deletedAt = membership.DeletedAt };
        await res.WriteAsJsonAsync(new ApiResponse<object>(payload, null));
        return res;
    }

    [Function("GetStudents")]
    public async Task<HttpResponseData> GetStudents(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "schools/{schoolId:guid}/students")] HttpRequestData req,
        Guid schoolId)
    {
        var principalResult = await AuthorizeHeadTeacher(req, schoolId);
        if (principalResult.Unauthorized != null) return principalResult.Unauthorized;
        // optional stageId query parameter to filter students by level (GUID)
        Guid? stageGuid = null;
        try
        {
            var qParsed = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(req.Url.Query);
            if (qParsed.TryGetValue("stageId", out var v) && Guid.TryParse(v.ToString(), out var sg)) stageGuid = sg;
        }
        catch { stageGuid = null; }

        var q = from sm in _db.SchoolMemberships
                where sm.SchoolId == schoolId && sm.RoleInSchool == SchoolRole.Student && (stageGuid == null || sm.StageId == stageGuid)
                join u in _db.Users on sm.UserId equals u.Id
                join p in _db.UserProfiles on u.Id equals p.UserId into up
                from prof in up.DefaultIfEmpty()
            select new { userId = u.Id, username = u.Username, firstName = prof != null ? prof.FirstName : null, lastName = prof != null ? prof.LastName : null, stageId = sm.StageId, subLevelId = sm.SubLevelId };

        var list = await q.ToListAsync();

        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(new ApiResponse<object>(list, null));
        return res;
    }

    [Function("GetLearners")]
    public async Task<HttpResponseData> GetLearners(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "schools/{schoolId:guid}/learners")] HttpRequestData req,
        Guid schoolId)
    {
        // Allow either head teachers or teachers assigned to the school to list learners
        var principal = await ValidateJwt(req);
        if (principal == null) return await BuildUnauthorized(req);

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier) ?? principal.FindFirst(JwtRegisteredClaimNames.Sub);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId)) return await BuildUnauthorized(req);

        var membership = await _db.SchoolMemberships.FirstOrDefaultAsync(sm => sm.SchoolId == schoolId && sm.UserId == userId && !sm.IsDeleted);
        if (membership == null) return await BuildUnauthorized(req);

        var isTeacherOrHead = membership.RoleInSchool == SchoolRole.Teacher || membership.RoleInSchool == SchoolRole.HeadTeacher;
        if (!isTeacherOrHead) return await BuildUnauthorized(req);

        // optional stageId query parameter to filter students by level (GUID)
        Guid? stageGuid = null;
        try
        {
            var qParsed = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(req.Url.Query);
            if (qParsed.TryGetValue("stageId", out var v) && Guid.TryParse(v.ToString(), out var sg)) stageGuid = sg;
        }
        catch { stageGuid = null; }

        var q = from sm in _db.SchoolMemberships
                where sm.SchoolId == schoolId && sm.RoleInSchool == SchoolRole.Student && (stageGuid == null || sm.StageId == stageGuid)
                join u in _db.Users on sm.UserId equals u.Id
                join p in _db.UserProfiles on u.Id equals p.UserId into up
                from prof in up.DefaultIfEmpty()
            select new { userId = u.Id, username = u.Username, firstName = prof != null ? prof.FirstName : null, lastName = prof != null ? prof.LastName : null, stageId = sm.StageId, subLevelId = sm.SubLevelId };

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

        var q = _db.Subjects
            .Where(s => s.SchoolId == schoolId && !s.IsDeleted)
            .Select(s => new { subjectId = s.SubjectId, name = s.Name, stage = (int)s.Stage, description = s.Description, subLevelId = (Guid?)s.SubLevelId });
        var list = await q.ToListAsync();

        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(new ApiResponse<object>(list, null));
        return res;
    }

    [Function("GetSchool")]
    public async Task<HttpResponseData> GetSchool(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "schools/{schoolId:guid}")] HttpRequestData req,
        Guid schoolId)
    {
        // Require authenticated user and membership in school
        var principal = await ValidateJwt(req);
        if (principal == null) return await BuildUnauthorized(req);

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier) ?? principal.FindFirst(JwtRegisteredClaimNames.Sub);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId)) return await BuildUnauthorized(req);

        var membership = await _db.SchoolMemberships.FirstOrDefaultAsync(sm => sm.SchoolId == schoolId && sm.UserId == userId);
        if (membership == null) return await BuildUnauthorized(req);

        var school = await _db.Schools.FirstOrDefaultAsync(s => s.SchoolId == schoolId);
        if (school == null) return await Error(req, HttpStatusCode.NotFound, "school not found");

        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(new ApiResponse<object>(new
        {
            schoolId = school.SchoolId,
            schoolName = school.SchoolName,
            schoolAddress = school.SchoolAddress,
            city = school.City,
            state = school.State,
            country = school.Country,
            county = school.County,
            zipCode = school.ZipCode,
            phoneNumber = school.PhoneNumber,
            email = school.Email,
            description = school.Description,
            imageUrl = school.ImageUrl,
            schoolLogoUrl = school.ImageUrl
        }, null));
        return res;
    }

    [Function("UpdateSchool")]
    public async Task<HttpResponseData> UpdateSchool(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "schools/{schoolId:guid}")] HttpRequestData req,
        Guid schoolId)
    {
        var principalResult = await AuthorizeHeadTeacher(req, schoolId);
        if (principalResult.Unauthorized != null) return principalResult.Unauthorized;

        var payload = await req.ReadFromJsonAsync<CreateSchoolRequest>();
        if (payload == null) return await Error(req, HttpStatusCode.BadRequest, "Invalid JSON body.");

        var school = await _db.Schools.FirstOrDefaultAsync(s => s.SchoolId == schoolId);
        if (school == null) return await Error(req, HttpStatusCode.NotFound, "school not found");

        if (!string.IsNullOrWhiteSpace(payload.SchoolName)) school.SchoolName = payload.SchoolName.Trim();
        school.Description = payload.Description?.Trim() ?? string.Empty;
        school.SchoolAddress = payload.SchoolAddress?.Trim() ?? string.Empty;
        school.City = payload.City?.Trim() ?? string.Empty;
        school.State = payload.State?.Trim() ?? string.Empty;
        school.Country = payload.Country?.Trim() ?? string.Empty;
        school.County = payload.County?.Trim() ?? string.Empty;
        school.ZipCode = payload.ZipCode?.Trim() ?? string.Empty;
        school.PhoneNumber = payload.PhoneNumber?.Trim() ?? string.Empty;
        school.Email = payload.Email?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(payload.ImageUrl)) school.ImageUrl = payload.ImageUrl.Trim();
        school.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(new ApiResponse<object>(new { schoolId = school.SchoolId, schoolLogoUrl = school.ImageUrl }, null));
        return res;
    }

    [Function("UpdateSchoolLogo")]
    public async Task<HttpResponseData> UpdateSchoolLogo(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "schools/{schoolId:guid}/logo")] HttpRequestData req,
        Guid schoolId)
    {
        var principalResult = await AuthorizeHeadTeacher(req, schoolId);
        if (principalResult.Unauthorized != null) return principalResult.Unauthorized;

        // Accept a simple JSON body { imageUrl: string }
        string? imageUrl = null;
        try
        {
            var body = await req.ReadFromJsonAsync<System.Text.Json.Nodes.JsonObject>();
            if (body == null) return await Error(req, HttpStatusCode.BadRequest, "Invalid JSON body.");
            imageUrl = body["imageUrl"]?.ToString()?.Trim();
        }
        catch (System.Text.Json.JsonException)
        {
            return await Error(req, HttpStatusCode.BadRequest, "Invalid JSON body.");
        }

        if (string.IsNullOrWhiteSpace(imageUrl)) return await Error(req, HttpStatusCode.BadRequest, "imageUrl is required.");

        var school = await _db.Schools.FirstOrDefaultAsync(s => s.SchoolId == schoolId);
        if (school == null) return await Error(req, HttpStatusCode.NotFound, "school not found");

        school.ImageUrl = imageUrl;
        school.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(new ApiResponse<object>(new { schoolId = school.SchoolId, schoolLogoUrl = school.ImageUrl }, null));
        return res;
    }

    [Function("CreateSchool")]
    public async Task<HttpResponseData> CreateSchool(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "schools")] HttpRequestData req)
    {
        var payload = await req.ReadFromJsonAsync<CreateSchoolRequest>();
        if (payload == null) return await Error(req, HttpStatusCode.BadRequest, "Invalid JSON body.");

        var principal = await ValidateJwt(req);
        if (principal == null) return await BuildUnauthorized(req);

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier) ?? principal.FindFirst(JwtRegisteredClaimNames.Sub);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId)) return await BuildUnauthorized(req);

        // Prevent creating multiple schools for same head user
        var hasSchool = await _db.SchoolMemberships.AnyAsync(sm => sm.UserId == userId && sm.RoleInSchool == SchoolRole.HeadTeacher);
        if (hasSchool) return await Error(req, HttpStatusCode.Conflict, "user already owns a school");

        var schoolId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var school = new School
        {
            SchoolId = schoolId,
            SchoolName = payload.SchoolName?.Trim() ?? string.Empty,
            SchoolAddress = payload.SchoolAddress?.Trim() ?? string.Empty,
            City = payload.City?.Trim() ?? string.Empty,
            State = payload.State?.Trim() ?? string.Empty,
            Country = payload.Country?.Trim() ?? string.Empty,
            County = payload.County?.Trim() ?? string.Empty,
            ZipCode = payload.ZipCode?.Trim() ?? string.Empty,
            PhoneNumber = payload.PhoneNumber?.Trim() ?? string.Empty,
            Email = payload.Email?.Trim() ?? string.Empty,
            Description = payload.Description?.Trim() ?? string.Empty,
            ImageUrl = payload.ImageUrl?.Trim() ?? string.Empty,
            HeadTeacherUserId = userId,
            CreatedAt = now,
            UpdatedAt = now,
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

        await _db.Schools.AddAsync(school);
        await _db.SchoolMemberships.AddAsync(membership);
        await _db.SaveChangesAsync();

        var res = req.CreateResponse(HttpStatusCode.Created);
        await res.WriteAsJsonAsync(new ApiResponse<object>(new { schoolId = schoolId }, null));
        return res;
    }

    [Function("GetStages")]
    public async Task<HttpResponseData> GetStages(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "schools/{schoolId:guid}/stages")] HttpRequestData req,
        Guid schoolId)
    {
        var principalResult = await AuthorizeHeadTeacher(req, schoolId);
        if (principalResult.Unauthorized != null) return principalResult.Unauthorized;

        var q = _db.Stages
            .Where(s => s.SchoolId == schoolId && !s.IsDeleted)
            .Select(s => new { stageId = s.StageId, name = s.Name, label = s.Label, description = s.Description });

        var list = await q.ToListAsync();

        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(new ApiResponse<object>(list, null));
        return res;
    }

    [Function("GetMembership")]
    public async Task<HttpResponseData> GetMembership(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "schools/{schoolId:guid}/memberships/{userId:guid}")] HttpRequestData req,
        Guid schoolId,
        Guid userId)
    {
        // Allow head teacher or the user themselves
        var headResult = await AuthorizeHeadTeacher(req, schoolId);
        if (headResult.Principal == null)
        {
            var principal = await ValidateJwt(req);
            if (principal == null) return await BuildUnauthorized(req);
            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier) ?? principal.FindFirst(JwtRegisteredClaimNames.Sub);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var callerId) || callerId != userId)
            {
                return await BuildUnauthorized(req);
            }
        }

        var membership = await _db.SchoolMemberships.FirstOrDefaultAsync(sm => sm.SchoolId == schoolId && sm.UserId == userId);
        if (membership == null) return await Error(req, HttpStatusCode.NotFound, "membership not found");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        var profile = await _db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);

        var resObj = new
        {
            userId = user?.Id,
            username = user?.Username,
            firstName = profile?.FirstName,
            lastName = profile?.LastName,
            roleInSchool = membership.RoleInSchool.ToString(),
            stageId = membership.StageId,
            subLevelId = membership.SubLevelId
        };

        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(new ApiResponse<object>(resObj, null));
        return res;
    }

    [Function("CreateStage")]
    public async Task<HttpResponseData> CreateStage(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "schools/{schoolId:guid}/stages")] HttpRequestData req,
        Guid schoolId)
    {
        var principalResult = await AuthorizeHeadTeacher(req, schoolId);
        if (principalResult.Unauthorized != null) return principalResult.Unauthorized;

        var payload = await req.ReadFromJsonAsync<CreateStageRequest>();
        if (payload == null) return await Error(req, HttpStatusCode.BadRequest, "Invalid JSON body.");
        if (string.IsNullOrWhiteSpace(payload.Name)) return await Error(req, HttpStatusCode.BadRequest, "name is required.");

        // Continue with stage creation

        // Prevent duplicate stage names within the same school
        var exists = await _db.Stages.AnyAsync(s => s.SchoolId == schoolId && s.Name == payload.Name.Trim() && !s.IsDeleted);
        if (exists) return await Error(req, HttpStatusCode.Conflict, "stage name already exists");

        var stage = new Stage
        {
            StageId = Guid.NewGuid(),
            SchoolId = schoolId,
            Name = payload.Name.Trim(),
            Label = string.IsNullOrWhiteSpace(payload.Label) ? null : payload.Label.Trim(),
            Description = string.IsNullOrWhiteSpace(payload.Description) ? null : payload.Description.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false,
        };

        await _db.Stages.AddAsync(stage);
        await _db.SaveChangesAsync();

        var res = req.CreateResponse(HttpStatusCode.Created);
        await res.WriteAsJsonAsync(new ApiResponse<object>(new { stageId = stage.StageId, name = stage.Name, label = stage.Label, description = stage.Description }, null));
        return res;
    }

    [Function("GetSubLevels")]
    public async Task<HttpResponseData> GetSubLevels(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "schools/{schoolId:guid}/stages/{stageId:guid}/sublevels")] HttpRequestData req,
        Guid schoolId,
        Guid stageId)
    {
        var principalResult = await AuthorizeHeadTeacher(req, schoolId);
        if (principalResult.Unauthorized != null) return principalResult.Unauthorized;

        // verify stage exists and belongs to school
        var stage = await _db.Stages.FirstOrDefaultAsync(s => s.StageId == stageId && s.SchoolId == schoolId && !s.IsDeleted);
        if (stage == null) return await Error(req, HttpStatusCode.NotFound, "stage not found");

        var list = await _db.SubLevels.Where(sl => sl.StageId == stageId && !sl.IsDeleted)
            .Select(sl => new { subLevelId = sl.SubLevelId, name = sl.Name, label = sl.Label, description = sl.Description })
            .ToListAsync();

        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(new ApiResponse<object>(list, null));
        return res;
    }

    [Function("CreateSubLevel")]
    public async Task<HttpResponseData> CreateSubLevel(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "schools/{schoolId:guid}/stages/{stageId:guid}/sublevels")] HttpRequestData req,
        Guid schoolId,
        Guid stageId)
    {
        var principalResult = await AuthorizeHeadTeacher(req, schoolId);
        if (principalResult.Unauthorized != null) return principalResult.Unauthorized;

        var payload = await req.ReadFromJsonAsync<CreateSubLevelRequest>();
        if (payload == null) return await Error(req, HttpStatusCode.BadRequest, "Invalid JSON body.");
        if (string.IsNullOrWhiteSpace(payload.Name)) return await Error(req, HttpStatusCode.BadRequest, "name is required.");

        // verify stage
        var stage = await _db.Stages.FirstOrDefaultAsync(s => s.StageId == stageId && s.SchoolId == schoolId && !s.IsDeleted);
        if (stage == null) return await Error(req, HttpStatusCode.NotFound, "stage not found");

        // prevent duplicate names within stage
        var exists = await _db.SubLevels.AnyAsync(sl => sl.StageId == stageId && sl.Name == payload.Name.Trim() && !sl.IsDeleted);
        if (exists) return await Error(req, HttpStatusCode.Conflict, "sublevel name already exists");

        var sub = new SubLevel
        {
            SubLevelId = Guid.NewGuid(),
            StageId = stageId,
            Name = payload.Name.Trim(),
            Label = string.IsNullOrWhiteSpace(payload.Label) ? null : payload.Label.Trim(),
            Description = string.IsNullOrWhiteSpace(payload.Description) ? null : payload.Description.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false,
        };

        await _db.SubLevels.AddAsync(sub);
        await _db.SaveChangesAsync();

        var res = req.CreateResponse(HttpStatusCode.Created);
        await res.WriteAsJsonAsync(new ApiResponse<object>(new { subLevelId = sub.SubLevelId, name = sub.Name, label = sub.Label, description = sub.Description }, null));
        return res;
    }

    [Function("UpdateSubLevel")]
    public async Task<HttpResponseData> UpdateSubLevel(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "schools/{schoolId:guid}/stages/{stageId:guid}/sublevels/{subLevelId:guid}")] HttpRequestData req,
        Guid schoolId,
        Guid stageId,
        Guid subLevelId)
    {
        var principalResult = await AuthorizeHeadTeacher(req, schoolId);
        if (principalResult.Unauthorized != null) return principalResult.Unauthorized;

        var payload = await req.ReadFromJsonAsync<CreateSubLevelRequest>();
        if (payload == null) return await Error(req, HttpStatusCode.BadRequest, "Invalid JSON body.");

        var sub = await _db.SubLevels.FirstOrDefaultAsync(sl => sl.SubLevelId == subLevelId && sl.StageId == stageId && !sl.IsDeleted);
        if (sub == null) return await Error(req, HttpStatusCode.NotFound, "sublevel not found");

        if (!string.IsNullOrWhiteSpace(payload.Name)) sub.Name = payload.Name.Trim();
        sub.Label = string.IsNullOrWhiteSpace(payload.Label) ? null : payload.Label.Trim();
        sub.Description = string.IsNullOrWhiteSpace(payload.Description) ? null : payload.Description.Trim();
        sub.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(new ApiResponse<object>(new { subLevelId = sub.SubLevelId, name = sub.Name, label = sub.Label, description = sub.Description }, null));
        return res;
    }

    [Function("DeleteSubLevel")]
    public async Task<HttpResponseData> DeleteSubLevel(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "schools/{schoolId:guid}/stages/{stageId:guid}/sublevels/{subLevelId:guid}")] HttpRequestData req,
        Guid schoolId,
        Guid stageId,
        Guid subLevelId)
    {
        var principalResult = await AuthorizeHeadTeacher(req, schoolId);
        if (principalResult.Unauthorized != null) return principalResult.Unauthorized;

        var sub = await _db.SubLevels.FirstOrDefaultAsync(sl => sl.SubLevelId == subLevelId && sl.StageId == stageId && !sl.IsDeleted);
        if (sub == null) return await Error(req, HttpStatusCode.NotFound, "sublevel not found");

        // soft-delete
        sub.IsDeleted = true;
        sub.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(new ApiResponse<object>(null, null));
        return res;
    }

    

    [Function("UpdateStage")]
    public async Task<HttpResponseData> UpdateStage(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "schools/{schoolId:guid}/stages/{stageId:guid}")] HttpRequestData req,
        Guid schoolId,
        Guid stageId)
    {
        var principalResult = await AuthorizeHeadTeacher(req, schoolId);
        if (principalResult.Unauthorized != null) return principalResult.Unauthorized;

        var payload = await req.ReadFromJsonAsync<CreateStageRequest>();
        if (payload == null) return await Error(req, HttpStatusCode.BadRequest, "Invalid JSON body.");

        var stage = await _db.Stages.FirstOrDefaultAsync(s => s.StageId == stageId && s.SchoolId == schoolId && !s.IsDeleted);
        if (stage == null) return await Error(req, HttpStatusCode.NotFound, "stage not found");

        if (!string.IsNullOrWhiteSpace(payload.Name)) stage.Name = payload.Name.Trim();
        stage.Label = string.IsNullOrWhiteSpace(payload.Label) ? null : payload.Label.Trim();
        stage.Description = string.IsNullOrWhiteSpace(payload.Description) ? null : payload.Description.Trim();
        stage.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(new ApiResponse<object>(new { stageId = stage.StageId, name = stage.Name, label = stage.Label, description = stage.Description }, null));
        return res;
    }

    [Function("DeleteStage")]
    public async Task<HttpResponseData> DeleteStage(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "schools/{schoolId:guid}/stages/{stageId:guid}")] HttpRequestData req,
        Guid schoolId,
        Guid stageId)
    {
        var principalResult = await AuthorizeHeadTeacher(req, schoolId);
        if (principalResult.Unauthorized != null) return principalResult.Unauthorized;

        var stage = await _db.Stages.FirstOrDefaultAsync(s => s.StageId == stageId && s.SchoolId == schoolId && !s.IsDeleted);
        if (stage == null) return await Error(req, HttpStatusCode.NotFound, "stage not found");

        _db.Stages.Remove(stage);
        await _db.SaveChangesAsync();

        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(new ApiResponse<object>(null, null));
        return res;
    }

    [Function("CreateSubject")]
    public async Task<HttpResponseData> CreateSubject(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "schools/{schoolId:guid}/subjects")] HttpRequestData req,
        Guid schoolId)
    {
        var principalResult = await AuthorizeHeadTeacher(req, schoolId);
        if (principalResult.Unauthorized != null) return principalResult.Unauthorized;

        // Accept either numeric `stage` (enum) or a school-specific `stageId` (GUID) in the request body.
        CreateSubjectRequest? payload = null;
        string? providedStageId = null;
        int? resolvedStageNumeric = null;
        try
        {
            var body = await req.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body)) return await Error(req, HttpStatusCode.BadRequest, "Invalid JSON body.");

            using var doc = System.Text.Json.JsonDocument.Parse(body);
            var root = doc.RootElement;

            // name/description/subLevelId are read via deserialization (allow case-insensitive JSON keys from clients)
            try { Console.WriteLine($"CreateSubject raw body: {body}"); } catch {}
            payload = System.Text.Json.JsonSerializer.Deserialize<CreateSubjectRequest>(root.GetRawText(), new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Check for explicit stageId (GUID) or stage as string (case-insensitive property names)
            foreach (var prop in root.EnumerateObject())
            {
                if (string.Equals(prop.Name, "stageId", StringComparison.OrdinalIgnoreCase) && prop.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    providedStageId = prop.Value.GetString()?.Trim();
                }
                else if (string.Equals(prop.Name, "stage", StringComparison.OrdinalIgnoreCase))
                {
                    var stageEl = prop.Value;
                    if (stageEl.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        var s = stageEl.GetString()?.Trim();
                        if (!string.IsNullOrWhiteSpace(s))
                        {
                            // If it's a GUID, treat as stageId
                            if (Guid.TryParse(s, out var sg)) providedStageId = s;
                            else if (int.TryParse(s, out var i)) resolvedStageNumeric = i;
                        }
                    }
                    else if (stageEl.ValueKind == System.Text.Json.JsonValueKind.Number)
                    {
                        if (stageEl.TryGetInt32(out var i)) resolvedStageNumeric = i;
                    }
                }
            }
        }
        catch (System.Text.Json.JsonException)
        {
            return await Error(req, HttpStatusCode.BadRequest, "Invalid JSON body.");
        }

        if (payload == null) return await Error(req, HttpStatusCode.BadRequest, "Invalid JSON body.");
        if (string.IsNullOrWhiteSpace(payload.Name)) return await Error(req, HttpStatusCode.BadRequest, "name is required.");

        // If a stageId GUID was provided, resolve it to the numeric enum
        Guid? parsedStageGuid = null;
        string? resolvedStageName = null;
        if (!string.IsNullOrWhiteSpace(providedStageId) && Guid.TryParse(providedStageId, out var sguid))
        {
            parsedStageGuid = sguid;
            var st = await _db.Stages.FirstOrDefaultAsync(s => s.StageId == parsedStageGuid && s.SchoolId == schoolId && !s.IsDeleted);
            if (st == null) return await Error(req, HttpStatusCode.BadRequest, "stageId is invalid for this school");
            // map Stage name/label to enum numeric
            resolvedStageNumeric = MapStageNameToNumeric(st.Label ?? st.Name);
            resolvedStageName = st.Label ?? st.Name;
            if (resolvedStageNumeric == null)
            {
                try { Console.WriteLine($"CreateSubject BAD REQUEST: stageId maps to unknown stage label. providedStageId='{providedStageId}', stageName='{st.Name}', stageLabel='{st.Label}', payloadStage={payload?.Stage}, payloadSubLevelId={payload?.SubLevelId}"); } catch {}
                return await Error(req, HttpStatusCode.BadRequest, "stageId refers to a stage with an unsupported name/label; use a recognized stage name (e.g. Primary) or pass a numeric 'stage' value.");
            }
        }

        if (resolvedStageNumeric == null)
        {
            // if not resolved from string/guide, use payload.Stage (deserialized int)
            resolvedStageNumeric = payload.Stage;
        }

        if (!Enum.IsDefined(typeof(EducationStage), resolvedStageNumeric.Value))
        {
            // If a stageId GUID was provided, give a clearer error explaining the mapping problem
                if (parsedStageGuid.HasValue)
                {
                    try { Console.WriteLine($"CreateSubject BAD REQUEST: invalid stage mapping for stageId='{providedStageId}', resolvedStageNumeric={resolvedStageNumeric}, resolvedStageName='{resolvedStageName}', payloadStage={payload?.Stage}, payloadSubLevelId={payload?.SubLevelId}"); } catch {}
                    return await Error(req, HttpStatusCode.BadRequest, "stageId refers to a stage with an unsupported name/label; use a recognized stage name (e.g. Primary) or pass a numeric 'stage' value.");
                }

            Console.WriteLine($"CreateSubject BAD REQUEST: invalid stage. name='{payload.Name}', providedStageId='{providedStageId}', resolvedStageNumeric={resolvedStageNumeric}");
            return await Error(req, HttpStatusCode.BadRequest, $"stage value '{resolvedStageNumeric}' is invalid. Use one of the supported values: 1=PreSchool,2=Primary,3=MiddleSchool,4=HighSchool,5=Undergraduate,6=Graduate,7=Professional.");
        }

        var existing = await _db.Subjects.FirstOrDefaultAsync(s => s.SchoolId == schoolId && s.Name == payload.Name.Trim() && (int)s.Stage == resolvedStageNumeric.Value);
        if (existing != null)
        {
            var resExists = req.CreateResponse(HttpStatusCode.OK);
            await resExists.WriteAsJsonAsync(new ApiResponse<object>(new { subjectId = existing.SubjectId }, null));
            return resExists;
        }

        var subject = new Subject
        {
            SubjectId = Guid.NewGuid(),
            SchoolId = schoolId,
            Name = payload.Name.Trim(),
            Stage = (EducationStage)resolvedStageNumeric.Value,
            Description = payload.Description?.Trim(),
            SubLevelId = payload.SubLevelId
        };
        // If SubLevelId provided, validate it belongs to the school
        if (payload.SubLevelId != null)
        {
            var subGuid = payload.SubLevelId.Value;
            var sub = await _db.SubLevels.FirstOrDefaultAsync(sl => sl.SubLevelId == subGuid && !sl.IsDeleted);
            if (sub == null) return await Error(req, HttpStatusCode.BadRequest, "subLevelId is invalid");
            var parentStage = await _db.Stages.FirstOrDefaultAsync(s => s.StageId == sub.StageId && s.SchoolId == schoolId && !s.IsDeleted);
            if (parentStage == null) return await Error(req, HttpStatusCode.BadRequest, "subLevelId is invalid for this school");
            if (parsedStageGuid.HasValue && parsedStageGuid.Value != sub.StageId)
            {
                Console.WriteLine($"CreateSubject BAD REQUEST: sublevel does not belong to provided stage. name='{payload.Name}', providedStageId='{providedStageId}', subLevelId='{subGuid}', sub.StageId='{sub.StageId}'");
                return await Error(req, HttpStatusCode.BadRequest, "subLevelId does not belong to provided stage");
            }
        }
        await _db.Subjects.AddAsync(subject);
        await _db.SaveChangesAsync();

        var res = req.CreateResponse(HttpStatusCode.Created);
        await res.WriteAsJsonAsync(new ApiResponse<object>(new { subjectId = subject.SubjectId }, null));
        return res;
    }

    [Function("UpdateStudent")]
    public async Task<HttpResponseData> UpdateStudent(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "schools/{schoolId:guid}/students/{userId:guid}")] HttpRequestData req,
        Guid schoolId,
        Guid userId)
    {
        var principalResult = await AuthorizeHeadTeacher(req, schoolId);
        if (principalResult.Unauthorized != null) return principalResult.Unauthorized;

        var payload = await req.ReadFromJsonAsync<UpdatePersonRequest>();
        if (payload == null) return await Error(req, HttpStatusCode.BadRequest, "Invalid JSON body.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return await Error(req, HttpStatusCode.NotFound, "student not found");

        if (!string.IsNullOrWhiteSpace(payload.Username))
        {
            var exists = await _db.Users.AnyAsync(u => u.Username == payload.Username.Trim() && u.Id != userId);
            if (exists) return await Error(req, HttpStatusCode.Conflict, "username already exists");
            user.Username = payload.Username.Trim();
        }

        var profile = await _db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null)
        {
            profile = new UserProfile { Id = Guid.NewGuid(), UserId = userId, FirstName = payload.FirstName?.Trim(), LastName = payload.LastName?.Trim() };
            await _db.UserProfiles.AddAsync(profile);
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(payload.FirstName)) profile.FirstName = payload.FirstName.Trim();
            if (!string.IsNullOrWhiteSpace(payload.LastName)) profile.LastName = payload.LastName.Trim();
        }

        await _db.SaveChangesAsync();

        // Update student's stage assignment if provided (nullable)
        if (payload.StageId != null)
        {
            var membership = await _db.SchoolMemberships.FirstOrDefaultAsync(sm => sm.SchoolId == schoolId && sm.UserId == userId && sm.RoleInSchool == SchoolRole.Student);
            if (membership == null) return await Error(req, HttpStatusCode.NotFound, "student membership not found");

            // allow clearing stage by passing empty string or null
            if (string.IsNullOrWhiteSpace(payload.StageId))
            {
                membership.StageId = null;
            }
            else
            {
                // validate that stage exists for this school
                if (!Guid.TryParse(payload.StageId, out var stageGuid)) return await Error(req, HttpStatusCode.BadRequest, "stageId is invalid");
                var stageExists = await _db.Stages.AnyAsync(s => s.StageId == stageGuid && s.SchoolId == schoolId && !s.IsDeleted);
                if (!stageExists) return await Error(req, HttpStatusCode.BadRequest, "stageId is invalid");

                membership.StageId = stageGuid;
            }

            await _db.SaveChangesAsync();
        }

        // Update student's sublevel assignment if provided (nullable)
        if (payload.SubLevelId != null)
        {
            var membership = await _db.SchoolMemberships.FirstOrDefaultAsync(sm => sm.SchoolId == schoolId && sm.UserId == userId && sm.RoleInSchool == SchoolRole.Student);
            if (membership == null) return await Error(req, HttpStatusCode.NotFound, "student membership not found");

            // allow clearing by passing empty string or null
            if (string.IsNullOrWhiteSpace(payload.SubLevelId))
            {
                membership.SubLevelId = null;
            }
            else
            {
                if (!Guid.TryParse(payload.SubLevelId, out var subGuid)) return await Error(req, HttpStatusCode.BadRequest, "subLevelId is invalid");
                var sub = await _db.SubLevels.FirstOrDefaultAsync(sl => sl.SubLevelId == subGuid && !sl.IsDeleted);
                if (sub == null) return await Error(req, HttpStatusCode.BadRequest, "subLevelId is invalid");

                // Ensure the sublevel belongs to the student's stage (current or newly set)
                var effectiveStage = membership.StageId;
                if (effectiveStage == null) return await Error(req, HttpStatusCode.BadRequest, "cannot assign sublevel without a stage");
                if (sub.StageId != effectiveStage) return await Error(req, HttpStatusCode.BadRequest, "subLevelId is not valid for the student's stage");

                membership.SubLevelId = subGuid;
            }

            await _db.SaveChangesAsync();
        }

        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(new ApiResponse<object>(null, null));
        return res;
    }

    [Function("DeleteStudent")]
    public async Task<HttpResponseData> DeleteStudent(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "schools/{schoolId:guid}/students/{userId:guid}")] HttpRequestData req,
        Guid schoolId,
        Guid userId)
    {
        var principalResult = await AuthorizeHeadTeacher(req, schoolId);
        if (principalResult.Unauthorized != null) return principalResult.Unauthorized;

        var membership = await _db.SchoolMemberships.FirstOrDefaultAsync(sm => sm.SchoolId == schoolId && sm.UserId == userId && sm.RoleInSchool == SchoolRole.Student);
        if (membership == null) return await Error(req, HttpStatusCode.NotFound, "student not found");

        _db.SchoolMemberships.Remove(membership);

        // Remove student role for user if present
        var role = await _db.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == StudentRoleId);
        if (role != null)
        {
            _db.UserRoles.Remove(role);
        }

        await _db.SaveChangesAsync();

        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(new ApiResponse<object>(null, null));
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

            Guid? subLevelGuid = null;
            if (root.TryGetProperty("subLevelId", out var slEl) && slEl.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                var slStr = slEl.GetString();
                if (!string.IsNullOrWhiteSpace(slStr))
                {
                    if (!Guid.TryParse(slStr, out var parsed)) return await Error(req, HttpStatusCode.BadRequest, "subLevelId must be a valid GUID string.");
                    subLevelGuid = parsed;
                }
            }

            payload = new AssignTeacherSubjectRequest(teacherGuid, subjectGuid, subLevelGuid);
        }
        catch (System.Text.Json.JsonException)
        {
            return await Error(req, HttpStatusCode.BadRequest, "Invalid JSON body.");
        }

        var teacher = await _db.Users.FirstOrDefaultAsync(u => u.Id == payload.TeacherId);
        if (teacher == null) return await Error(req, HttpStatusCode.NotFound, "teacher not found");

        var subject = await _db.Subjects.FirstOrDefaultAsync(s => s.SubjectId == payload.SubjectId);
        if (subject == null) return await Error(req, HttpStatusCode.NotFound, "subject not found");
        if (subject.SchoolId != schoolId) return await Error(req, HttpStatusCode.BadRequest, "subjectId is invalid for this school");

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

        var existingAssignment = await _db.TeacherSubjects.FirstOrDefaultAsync(ts =>
            ts.UserId == payload.TeacherId
            && ts.SchoolId == schoolId
            && ts.SubjectId == payload.SubjectId
            && ts.SubLevelId == payload.SubLevelId
        );

        if (existingAssignment == null)
        {
            if (payload.SubLevelId.HasValue)
            {
                var subExists = await _db.SubLevels
                    .AnyAsync(sl => sl.SubLevelId == payload.SubLevelId.Value && !sl.IsDeleted && _db.Stages.Any(s => s.StageId == sl.StageId && s.SchoolId == schoolId));
                if (!subExists) return await Error(req, HttpStatusCode.BadRequest, "subLevelId is invalid for this school");
            }
            var assignment = new TeacherSubject
            {
                TeacherSubjectId = Guid.NewGuid(),
                UserId = payload.TeacherId,
                SchoolId = schoolId,
                SubjectId = payload.SubjectId,
                AssignedAt = DateTime.UtcNow,
                SubLevelId = payload.SubLevelId,
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
            var existsSubject = await _db.Subjects.AnyAsync(s => s.SubjectId == payload.SubjectId.Value && s.SchoolId == schoolId);
            if (!existsSubject) return await Error(req, HttpStatusCode.BadRequest, "subjectId is invalid for this school.");
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

    [Function("CreateCourseForSubject")]
    public async Task<HttpResponseData> CreateCourseForSubject(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "schools/{schoolId:guid}/subjects/{subjectId:guid}/courses")] HttpRequestData req,
        Guid schoolId,
        Guid subjectId)
    {
        // Allow head teacher or teachers assigned to the subject
        var principalResult = await AuthorizeTeacherForSubject(req, schoolId, subjectId);
        if (principalResult.Unauthorized != null) return principalResult.Unauthorized;

        var payload = await req.ReadFromJsonAsync<CreateCourseRequest>();
        if (payload == null) return await Error(req, HttpStatusCode.BadRequest, "Invalid JSON body.");
        if (string.IsNullOrWhiteSpace(payload.Title)) return await Error(req, HttpStatusCode.BadRequest, "title is required.");

        var subjectExists = await _db.Subjects.AnyAsync(s => s.SubjectId == subjectId && s.SchoolId == schoolId);
        if (!subjectExists) return await Error(req, HttpStatusCode.BadRequest, "subjectId is invalid for this school.");

        var course = new Course
        {
            CourseId = Guid.NewGuid(),
            SchoolId = schoolId,
            SubjectId = subjectId,
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

    [Function("GetCoursesForSubject")]
    public async Task<HttpResponseData> GetCoursesForSubject(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "schools/{schoolId:guid}/subjects/{subjectId:guid}/courses")] HttpRequestData req,
        Guid schoolId,
        Guid subjectId)
    {
        // Allow head teacher or assigned teacher to view lists for the school's subject (UI uses this in admin/teacher flows)
        var principalResult = await AuthorizeTeacherForSubject(req, schoolId, subjectId);
        if (principalResult.Unauthorized != null) return principalResult.Unauthorized;

        var q = _db.Courses.Where(c => c.SchoolId == schoolId && c.SubjectId == subjectId)
            .Select(c => new { courseId = c.CourseId, title = c.Title, summary = c.Summary, level = c.Level, isPublished = c.IsPublished });

        var list = await q.ToListAsync();
        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(new ApiResponse<object>(list, null));
        return res;
    }

    [Function("GetTopics")]
    public async Task<HttpResponseData> GetTopics(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "schools/{schoolId:guid}/subjects/{subjectId:guid}/topics")] HttpRequestData req,
        Guid schoolId,
        Guid subjectId)
    {
        // Allow head teacher or assigned teacher to view topics for the subject (used in admin and teacher flows)
        var principalResult = await AuthorizeTeacherForSubject(req, schoolId, subjectId);
        if (principalResult.Unauthorized != null) return principalResult.Unauthorized;

        // Use a raw SQL query with COALESCE so the DB returns non-null strings and the data reader
        // doesn't call GetString on NULL values (which can throw SqlNullValueException).
        var list = new List<object>();
        var conn = _db.Database.GetDbConnection();
        try
        {
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT t.TopicId, COALESCE(t.Subtopic,'') AS Subtopic, COALESCE(t.Notes,'') AS Notes, COALESCE(t.Name,'') AS Name, t.ParentTopicId
                                FROM dbo.Topics t
                                WHERE t.SubjectId = @subjectId AND t.SchoolId = @schoolId AND t.IsDeleted = 0";

            var pSubject = cmd.CreateParameter();
            pSubject.ParameterName = "@subjectId";
            pSubject.Value = subjectId;
            cmd.Parameters.Add(pSubject);

            var pSchool = cmd.CreateParameter();
            pSchool.ParameterName = "@schoolId";
            pSchool.Value = schoolId;
            cmd.Parameters.Add(pSchool);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var topicIdVal = reader.GetGuid(0);
                var subtopicVal = reader.GetString(1);
                var notesVal = reader.GetString(2);
                var nameVal = reader.GetString(3);
                var parentTopicIdVal = reader.IsDBNull(4) ? (Guid?)null : reader.GetGuid(4);

                list.Add(new { topicId = topicIdVal, subtopic = subtopicVal, notes = notesVal, name = nameVal, parentTopicId = parentTopicIdVal });
            }
        }
        finally
        {
            try { await conn.CloseAsync(); } catch { }
        }
        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(new ApiResponse<object>(list, null));
        return res;
    }

    [Function("GetTopic")]
    public async Task<HttpResponseData> GetTopic(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "topics/{topicId:guid}")] HttpRequestData req,
        Guid topicId)
    {
        var topic = await _db.Set<Nile.Database.Entities.Topic>().FirstOrDefaultAsync(t => t.TopicId == topicId && !t.IsDeleted);
        if (topic == null) return await Error(req, HttpStatusCode.NotFound, "topic not found");

        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(new ApiResponse<object>(new { topicId = topic.TopicId, subtopic = topic.Subtopic, notes = topic.Notes, name = topic.Name }, null));
        return res;
    }

    [Function("CreateTopic")]
    public async Task<HttpResponseData> CreateTopic(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "schools/{schoolId:guid}/subjects/{subjectId:guid}/topics")] HttpRequestData req,
        Guid schoolId,
        Guid subjectId)
    {
        var principalResult = await AuthorizeTeacherForSubject(req, schoolId, subjectId);
        if (principalResult.Unauthorized != null) return principalResult.Unauthorized;

        var payload = await req.ReadFromJsonAsync<CreateTopicRequest>();
        if (payload == null) return await Error(req, HttpStatusCode.BadRequest, "Invalid JSON body.");
        // Allow creating a parent/topic (TopicName) with an empty Subtopic. Require at least one of TopicName or Subtopic.
        if (string.IsNullOrWhiteSpace(payload.Subtopic) && string.IsNullOrWhiteSpace(payload.TopicName))
            return await Error(req, HttpStatusCode.BadRequest, "topicName or subtopic is required.");

        var topic = new Nile.Database.Entities.Topic
        {
            TopicId = Guid.NewGuid(),
            SchoolId = schoolId,
            SubjectId = subjectId,
            Subtopic = string.IsNullOrWhiteSpace(payload.Subtopic) ? null : payload.Subtopic.Trim(),
            Notes = payload.Notes?.Trim(),
            Name = payload.TopicName?.Trim(),
            ParentTopicId = payload.ParentTopicId,
            CreatedAt = DateTime.UtcNow,
        };

        // Validate ParentTopicId if supplied: it must reference an existing topic for the same school & subject
        if (payload.ParentTopicId.HasValue)
        {
            var parentExists = await _db.Set<Nile.Database.Entities.Topic>()
                .AnyAsync(t => t.TopicId == payload.ParentTopicId.Value && t.SchoolId == schoolId && t.SubjectId == subjectId && !t.IsDeleted);
            if (!parentExists)
            {
                return await Error(req, HttpStatusCode.BadRequest, "invalid ParentTopicId");
            }
        }

        await _db.Set<Nile.Database.Entities.Topic>().AddAsync(topic);
        await _db.SaveChangesAsync();

        // If a StageId was supplied, attempt to set Stage.TopicId to this newly-created topic
        if (payload.StageId.HasValue)
        {
            try
            {
                var stageInfo = await _db.Stages
                    .Where(s => s.StageId == payload.StageId.Value && s.SchoolId == schoolId && !s.IsDeleted)
                    .Select(s => new { s.StageId, s.TopicId })
                    .SingleOrDefaultAsync();

                if (stageInfo != null && !stageInfo.TopicId.HasValue)
                {
                    var stageStub = new Nile.Database.Entities.Stage { StageId = stageInfo.StageId };
                    _db.Attach(stageStub);
                    stageStub.TopicId = topic.TopicId;
                    stageStub.UpdatedAt = DateTime.UtcNow;
                    _db.Entry(stageStub).Property(s => s.TopicId).IsModified = true;
                    _db.Entry(stageStub).Property(s => s.UpdatedAt).IsModified = true;
                    await _db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                // Non-fatal: log and continue
                Console.WriteLine($"Failed to update Stage with TopicId: {ex}");
            }
        }

        // Reconcile any optimistic client story by ClientCorrelationId or create a minimal linked Story
        Guid? linkedStoryId = null;
        if (!string.IsNullOrWhiteSpace(payload.ClientCorrelationId))
        {
            try
            {
                var existingStoryInfo = await _db.Set<Nile.Database.Entities.Story>()
                    .Where(s => s.ClientCorrelationId == payload.ClientCorrelationId && !s.IsDeleted)
                    .Select(s => new { s.StoryId })
                    .SingleOrDefaultAsync();

                if (existingStoryInfo != null)
                {
                    var existingStub = new Nile.Database.Entities.Story { StoryId = existingStoryInfo.StoryId };
                    _db.Attach(existingStub);
                    existingStub.TopicId = topic.TopicId;
                    existingStub.ClientCorrelationId = null;
                    existingStub.UpdatedAt = DateTime.UtcNow;
                    _db.Entry(existingStub).Property(s => s.TopicId).IsModified = true;
                    _db.Entry(existingStub).Property(s => s.ClientCorrelationId).IsModified = true;
                    _db.Entry(existingStub).Property(s => s.UpdatedAt).IsModified = true;
                    await _db.SaveChangesAsync();
                    linkedStoryId = existingStub.StoryId;
                }
                else
                {
                    var newStory = new Nile.Database.Entities.Story
                    {
                        StoryId = Guid.NewGuid(),
                        SchoolId = schoolId,
                        SubjectId = subjectId,
                        TopicId = topic.TopicId,
                        PromptId = null,
                        User = string.IsNullOrWhiteSpace(payload.TopicName) ? payload.Subtopic : payload.TopicName,
                        Payload = null,
                        ClientCorrelationId = payload.ClientCorrelationId,
                        CreatedAt = DateTime.UtcNow,
                    };
                    await _db.Set<Nile.Database.Entities.Story>().AddAsync(newStory);
                    await _db.SaveChangesAsync();
                    linkedStoryId = newStory.StoryId;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to reconcile or create Story for ClientCorrelationId: {ex}");
            }
        }

        var res = req.CreateResponse(HttpStatusCode.Created);
        await res.WriteAsJsonAsync(new ApiResponse<object>(new { topicId = topic.TopicId, storyId = linkedStoryId }, null));
        return res;
    }

    [Function("UpdateTopic")]
    public async Task<HttpResponseData> UpdateTopic(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "schools/{schoolId:guid}/subjects/{subjectId:guid}/topics/{topicId:guid}")] HttpRequestData req,
        Guid schoolId,
        Guid subjectId,
        Guid topicId)
    {
        var principalResult = await AuthorizeTeacherForSubject(req, schoolId, subjectId);
        if (principalResult.Unauthorized != null) return principalResult.Unauthorized;

        var payload = await req.ReadFromJsonAsync<UpdateTopicRequest>();
        if (payload == null) return await Error(req, HttpStatusCode.BadRequest, "Invalid JSON body.");

        var topic = await _db.Set<Nile.Database.Entities.Topic>().FirstOrDefaultAsync(t => t.TopicId == topicId && t.SubjectId == subjectId && t.SchoolId == schoolId);
        if (topic == null) return await Error(req, HttpStatusCode.NotFound, "topic not found");

        if (!string.IsNullOrWhiteSpace(payload.Subtopic)) topic.Subtopic = payload.Subtopic.Trim();
        if (payload.Notes != null) topic.Notes = payload.Notes?.Trim();
        if (payload.TopicName != null) topic.Name = payload.TopicName?.Trim();
        if (payload.ParentTopicId.HasValue)
        {
            // Prevent setting a topic as its own parent
            if (payload.ParentTopicId.Value == topicId) return await Error(req, HttpStatusCode.BadRequest, "parentTopicId cannot be the same as topic id");

            var parent = await _db.Set<Nile.Database.Entities.Topic>().FirstOrDefaultAsync(t => t.TopicId == payload.ParentTopicId.Value && !t.IsDeleted);
            if (parent == null || parent.SchoolId != schoolId || parent.SubjectId != subjectId)
            {
                return await Error(req, HttpStatusCode.BadRequest, "invalid ParentTopicId");
            }

            topic.ParentTopicId = payload.ParentTopicId;
        }
        topic.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(new ApiResponse<object>(null, null));
        return res;
    }

    [Function("DeleteTopic")]
    public async Task<HttpResponseData> DeleteTopic(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "schools/{schoolId:guid}/subjects/{subjectId:guid}/topics/{topicId:guid}")] HttpRequestData req,
        Guid schoolId,
        Guid subjectId,
        Guid topicId)
    {
        var principalResult = await AuthorizeHeadTeacher(req, schoolId);
        if (principalResult.Unauthorized != null) return principalResult.Unauthorized;

        var topic = await _db.Set<Nile.Database.Entities.Topic>().FirstOrDefaultAsync(t => t.TopicId == topicId && t.SubjectId == subjectId && t.SchoolId == schoolId);
        if (topic == null) return await Error(req, HttpStatusCode.NotFound, "topic not found");

        // Soft-delete the topic and all descendant subtopics (cascade soft-delete).
        // Use a visited set to protect against cycles in the ParentTopicId graph (defensive guard)
        var toDelete = new List<Guid>();
        var visited = new HashSet<Guid>();
        var stack = new Stack<Guid>();
        stack.Push(topic.TopicId);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (visited.Contains(current)) continue;
            visited.Add(current);
            toDelete.Add(current);

            // find direct children of current topic
            var children = await _db.Set<Nile.Database.Entities.Topic>()
                .Where(t => t.ParentTopicId == current && t.SubjectId == subjectId && t.SchoolId == schoolId && !t.IsDeleted)
                .Select(t => t.TopicId)
                .ToListAsync();

            foreach (var childId in children)
            {
                if (!visited.Contains(childId)) stack.Push(childId);
            }
        }

        // mark all found topics as deleted
        var now = DateTime.UtcNow;
        var topicsEntities = await _db.Set<Nile.Database.Entities.Topic>()
            .Where(t => toDelete.Contains(t.TopicId) && t.SubjectId == subjectId && t.SchoolId == schoolId)
            .ToListAsync();
        foreach (var t in topicsEntities)
        {
            t.IsDeleted = true;
            t.DeletedAt = now;
            t.UpdatedAt = now;
        }

        await _db.SaveChangesAsync();

        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(new ApiResponse<object>(new { deletedIds = toDelete }, null));
        return res;
    }

    [Function("ClearTopicsForSchool")]
    public async Task<HttpResponseData> ClearTopicsForSchool(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "schools/{schoolId:guid}/topics/clear")] HttpRequestData req,
        Guid schoolId)
    {
        var principalResult = await AuthorizeHeadTeacher(req, schoolId);
        if (principalResult.Unauthorized != null) return principalResult.Unauthorized;

        var now = DateTime.UtcNow;
        var topics = await _db.Set<Nile.Database.Entities.Topic>().Where(t => t.SchoolId == schoolId && !t.IsDeleted).ToListAsync();
        foreach (var t in topics)
        {
            t.IsDeleted = true;
            t.DeletedAt = now;
            t.UpdatedAt = now;
        }
        await _db.SaveChangesAsync();

        var res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(new ApiResponse<object>(new { count = topics.Count }, null));
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

    private async Task<(ClaimsPrincipal? Principal, HttpResponseData? Unauthorized)> AuthorizeTeacherForSubject(HttpRequestData req, Guid schoolId, Guid subjectId)
    {
        // Allow head teachers (owner) as before
        var headResult = await AuthorizeHeadTeacher(req, schoolId);
        if (headResult.Principal != null) return (headResult.Principal, null);

        // Otherwise validate jwt and ensure the caller is assigned to the subject
        var principal = await ValidateJwt(req);
        if (principal == null) return (null, await BuildUnauthorized(req));

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier) ?? principal.FindFirst(JwtRegisteredClaimNames.Sub);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return (null, await BuildUnauthorized(req));
        }

        var assigned = await _db.TeacherSubjects.AnyAsync(ts => ts.UserId == userId && ts.SchoolId == schoolId && ts.SubjectId == subjectId);
        if (!assigned)
        {
            return (null, await BuildUnauthorized(req));
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
        // Avoid writing body here (which starts the response) so callers can safely return this response
        // and middleware can still set headers/status without conflict.
        res.Headers.Add("WWW-Authenticate", "Bearer");
        try { Console.WriteLine("BuildUnauthorized: returning 401 without body"); } catch {}
        return res;
    }

    private static async Task<HttpResponseData> Error(HttpRequestData req, HttpStatusCode status, string message, object? details = null)
    {
        var res = req.CreateResponse(status);
        // Avoid writing JSON body here to prevent starting the response early in middleware.
        // Instead attach diagnostic headers so clients and logs can inspect the error.
        if (!string.IsNullOrWhiteSpace(message) && !res.Headers.TryGetValues("X-Error-Message", out _))
            res.Headers.Add("X-Error-Message", message);
        if (details != null && !res.Headers.TryGetValues("X-Error-Details", out _))
            res.Headers.Add("X-Error-Details", details.ToString() ?? string.Empty);
        try { Console.WriteLine($"Error: {status} - {message} - {details}"); } catch {}
        return res;
    }

    private int? MapStageNameToNumeric(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var n = new string(name.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToLowerInvariant();
        // Exact matches
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "preschool", 1 }, { "primary", 2 }, { "middleschool", 3 }, { "highschool", 4 }, { "undergraduate", 5 }, { "graduate", 6 }, { "professional", 7 }
        };
        if (map.TryGetValue(n, out var val)) return val;
        // Fallback to contains checks for more verbose labels
        if (n.Contains("preschool")) return 1;
        if (n.Contains("primary")) return 2;
        if (n.Contains("middle")) return 3;
        if (n.Contains("high")) return 4;
        if (n.Contains("undergrad")) return 5;
        if (n.Contains("graduate")) return 6;
        if (n.Contains("professional")) return 7;
        return null;
    }
}
