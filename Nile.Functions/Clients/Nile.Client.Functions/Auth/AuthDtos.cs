namespace Nile.Client.Functions.Auth;

public record RegisterHeadRequest(string Username, string Password, string SchoolName, string FirstName, string LastName);
public record LoginRequest(string Username, string Password);
public record AuthResponse(string Token, Guid UserId, Guid? SchoolId, string Username, IEnumerable<string> Roles, string? SchoolName = null, string? SchoolLogoUrl = null, string? RedirectUrl = null);

public record ApiResponse<T>(T? Data, ApiError? Error);
public record ApiError(string Message, object? Details = null);
