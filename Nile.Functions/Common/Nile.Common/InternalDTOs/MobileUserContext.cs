using Nile.Common.Contexts;

namespace Nile.Common.InternalDTOs;

public class MobileUserContext : ContextBase
{
    public Guid UserId { get; init; }

    public string EmailAddress { get; init; } = null!;

    public string ExternalAuthId { get; init; } = null!;
}