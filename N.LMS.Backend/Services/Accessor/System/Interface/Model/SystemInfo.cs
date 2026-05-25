namespace N.LMS.Accessor.System.Interface.Model;

public sealed record SystemInfo
{
    public string Version { get; init; } = string.Empty;
    public string Environment { get; init; } = string.Empty;
    public string AssemblyName { get; init; } = string.Empty;
    public DateTime ServerTimeUtc { get; init; }
}

public sealed record SystemHealth
{
    public bool Healthy { get; init; }
    public bool DatabaseReachable { get; init; }
    public string? Message { get; init; }
    public DateTime CheckedAtUtc { get; init; }
}
