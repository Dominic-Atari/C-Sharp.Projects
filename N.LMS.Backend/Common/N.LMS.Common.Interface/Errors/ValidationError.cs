namespace N.LMS.Common.Interface.Errors;

public sealed record ValidationError : ErrorBase
{
    public string? FieldName { get; init; }
}
