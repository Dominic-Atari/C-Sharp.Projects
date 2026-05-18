using Nile.Managers.Contract.Client.DataContract;

namespace Nile.Managers.HealthCheck;

public sealed class TestMessage : RequestBase
{
    public required string Message { get; init; }
}