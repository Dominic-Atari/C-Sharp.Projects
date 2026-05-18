using System;

namespace Nile.Functions.Functions.Models;

public record CreateUserRequest(string Username, string FirstName, string LastName, string? Location);

public record UserResponse(Guid Id, string Username, string FirstName, string LastName, DateTimeOffset CreatedAt);

public record ApiResponse<T>(T? Data, ApiError? Error);

public record ApiError(string Message, object? Details = null);
