namespace N.LMS.Common.Interface.Errors;

public sealed record NotFoundError : ErrorBase
{
    public string? EntityName { get; init; }
    public string? FieldName { get; init; }
    public object? FieldValue { get; init; }
}
