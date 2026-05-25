namespace N.LMS.Common.Interface.Errors;

public sealed record ConflictError : ErrorBase
{
    public string? EntityName { get; init; }
    public string? FieldName { get; init; }
}
