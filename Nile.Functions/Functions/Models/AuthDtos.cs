namespace Nile.Functions.Functions.Models;

public record RegisterHeadRequest(string Username, string Password, string SchoolName, string FirstName, string LastName);
public record LoginRequest(string Username, string Password);
public record AuthResponse(string Token, Guid UserId, Guid? SchoolId, string Username, IEnumerable<string> Roles, string? RedirectUrl = null);

public record CreatePersonRequest(string Username, string Password, string FirstName, string LastName);
public record CreateSubjectRequest(string Name, int Stage, string? Description);
public record AssignTeacherSubjectRequest(Guid TeacherId, Guid SubjectId);
public record CreateCourseRequest(string Title, string? Summary, Guid? SubjectId, string? Level);
public record CreateLessonRequest(string Title, string? BodyMarkdown, string? ResourceUrl, int? DurationMinutes, int Order = 0);
