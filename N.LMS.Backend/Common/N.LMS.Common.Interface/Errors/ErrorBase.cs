using System.Text.Json.Serialization;

namespace N.LMS.Common.Interface.Errors;

public abstract record ErrorBase
{
    public string? Message { get; init; }
    public string? PublicMessage { get; init; }
    public string? Code { get; init; }

    [JsonIgnore]
    public Exception? Exception { get; init; }
}
