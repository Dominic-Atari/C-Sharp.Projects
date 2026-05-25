using System.Text.Json.Serialization;
using N.LMS.Common.Interface.Errors;

namespace N.LMS.Client.WebApi.Response;

public record ResponseBase
{
    [JsonIgnore]
    public ErrorBase[]? Errors { get; init; }
}
