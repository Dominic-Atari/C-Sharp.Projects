using N.LMS.Common.Interface.Errors;

namespace N.LMS.Common.Interface.Result;

public abstract record ResultBase
{
    public ErrorBase[]? Errors { get; init; }
    public bool IsSuccessful => Errors is null || Errors.Length == 0;
}
