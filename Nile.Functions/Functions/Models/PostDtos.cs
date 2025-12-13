using System;

namespace Nile.Functions.Functions.Models;

public record CreatePostRequest(Guid UserId, string Caption, string? ImageUrl);

public record PostResponse(Guid PostId, Guid UserId, string? Content, string? ImageUrl, DateTime CreatedAt);
