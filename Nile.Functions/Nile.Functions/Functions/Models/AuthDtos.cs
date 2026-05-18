namespace Nile.Functions.Functions.Models;

public record RegisterHeadRequest(string Username, string Password, string SchoolName, string FirstName, string LastName);
public record LoginRequest(string Username, string Password);
public record AuthResponse(string Token, Guid UserId, Guid? SchoolId, string Username, IEnumerable<string> Roles, string? SchoolName = null, string? SchoolLogoUrl = null, string? RedirectUrl = null);

public record CreatePersonRequest(string Username, string Password, string FirstName, string LastName, string? StageId = null, string? SubLevelId = null);
public record CreateSubjectRequest(string Name, int Stage, string? Description, Guid? SubLevelId = null);
public record AssignTeacherSubjectRequest(Guid TeacherId, Guid SubjectId, Guid? SubLevelId = null);
public record CreateCourseRequest(string Title, string? Summary, Guid? SubjectId, string? Level);
public record CreateLessonRequest(string Title, string? BodyMarkdown, string? ResourceUrl, int? DurationMinutes, int Order = 0);
public record CreateSchoolRequest(string SchoolName, string? Description, string? SchoolAddress, string? City, string? State, string? Country, string? County, string? ZipCode, string? PhoneNumber, string? Email, string? ImageUrl);
public record UpdatePersonRequest(string? Username, string? FirstName, string? LastName, string? StageId, string? SubLevelId);
public record CreateTopicRequest(string? Subtopic, string? Notes, string? TopicName, Guid? ParentTopicId, string? ClientCorrelationId, Guid? StageId);
public record UpdateTopicRequest(string? Subtopic, string? Notes, string? TopicName, Guid? ParentTopicId);
public record CreateStageRequest(string? StageId, string Name, string? Label, string? Description);
public record StageDetails(string StageId, string Name, string? Label, string? Description);
public record CreateSubLevelRequest(string? SubLevelId, string Name, string? Label, string? Description);
public record SubLevelDetails(string SubLevelId, string Name, string? Label, string? Description);
